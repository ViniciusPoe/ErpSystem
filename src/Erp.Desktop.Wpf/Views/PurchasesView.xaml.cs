using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Inventory;

namespace Erp.Desktop.Wpf.Views;

public partial class PurchasesView : UserControl
{
    private readonly InventoryApplicationService _service;
    private PurchaseSummary? _selectedPurchase;

    public PurchasesView(InventoryApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar compras", async () =>
        {
            PurchasesGrid.ItemsSource = await _service.GetPurchasesAsync();
            PrintPurchaseButton.IsEnabled = false;
        });
    }

    private void PurchasesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPurchase = PurchasesGrid.SelectedItem as PurchaseSummary;
        PrintPurchaseButton.IsEnabled = _selectedPurchase?.Status == "ENTREGUE";
    }

    private async void DeliveredButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPurchase is null) return;
        var id = _selectedPurchase.Id;

        await ViewHelper.RunSafeAsync("Marcar compra entregue", async () =>
        {
            await _service.MarkPurchaseDeliveredAsync(id);
            await ReloadAsync();

            // Busca a compra atualizada e gera PDF
            var purchases = await _service.GetPurchasesAsync();
            var delivered = purchases.FirstOrDefault(p => p.Id == id);
            if (delivered is not null)
                PdfHelper.GenerateAndOpenPurchasePdf(delivered);
        });
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPurchase is null) return;
        if (!ViewHelper.Confirm("Confirma o cancelamento desta compra?", "Cancelar Compra")) return;

        await ViewHelper.RunSafeAsync("Cancelar compra", async () =>
        {
            await _service.CancelPurchaseAsync(_selectedPurchase.Id);
            await ReloadAsync();
        });
    }

    private void PrintPurchaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPurchase is null) return;
        ViewHelper.RunSafe("Imprimir PDF", () => PdfHelper.GenerateAndOpenPurchasePdf(_selectedPurchase));
    }
}
