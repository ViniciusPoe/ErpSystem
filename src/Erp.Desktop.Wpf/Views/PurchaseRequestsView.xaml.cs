using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class PurchaseRequestsView : UserControl
{
    private readonly InventoryApplicationService _inventoryService;
    private readonly SalesApplicationService _salesService;

    public PurchaseRequestsView(InventoryApplicationService inventoryService, SalesApplicationService salesService)
    {
        InitializeComponent();
        _inventoryService = inventoryService;
        _salesService = salesService;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar solicitacoes", async () =>
        {
            ProductComboBox.ItemsSource = await _salesService.GetProductsAsync();

            var requests = await _inventoryService.GetPurchaseRequestsAsync();
            var purchases = await _inventoryService.GetPurchasesAsync();
            var purchaseByRequestId = purchases
                .Where(p => p.RequestId.HasValue)
                .ToDictionary(p => p.RequestId!.Value);

            RequestsGrid.ItemsSource = requests.Select(r =>
            {
                purchaseByRequestId.TryGetValue(r.Id, out var purchase);

                var approvalStatus = r.Status switch
                {
                    "ABERTA" => "PENDENTE",
                    "APROVADA" => "APROVADA",
                    "RECUSADA" => "RECUSADA",
                    _ => r.Status
                };

                var deliveryStatus = purchase?.Status switch
                {
                    "PENDENTE_ENTREGA" => "PENDENTE",
                    "ENTREGUE" => "ENTREGUE",
                    "CANCELADA" => "CANCELADA",
                    _ => null
                };

                var displayStatus = (approvalStatus, deliveryStatus) switch
                {
                    ("RECUSADA", _) => "RECUSADA",
                    ("APROVADA", "ENTREGUE") => "ENTREGUE",
                    ("APROVADA", "CANCELADA") => "CANCELADA",
                    ("APROVADA", _) => "AGUARDANDO_ENTREGA",
                    _ => "PENDENTE"
                };

                return new
                {
                    r.Id,
                    r.ProductSku,
                    r.RequestedQuantity,
                    ApprovalStatus = approvalStatus,
                    r.SupplierName,
                    r.ApprovedQuantity,
                    r.ApprovedUnitPrice,
                    DeliveryStatus = deliveryStatus,
                    r.Notes,
                    DisplayStatus = displayStatus
                };
            }).ToList();

            FormPanel.Visibility = Visibility.Collapsed;
        });
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        QuantityTextBox.Clear();
        NotesTextBox.Clear();
        ProductComboBox.SelectedItem = null;
        FormPanel.Visibility = Visibility.Visible;
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
    }

    private async void CreateRequestButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Criar solicitacao", async () =>
        {
            if (ProductComboBox.SelectedItem is not Product p) return;
            if (!ViewHelper.TryDecimal(QuantityTextBox.Text, out var qty) || qty <= 0) return;
            var notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
            await _inventoryService.CreatePurchaseRequestAsync(p.Id, qty, notes);
            QuantityTextBox.Clear();
            NotesTextBox.Clear();
            FormPanel.Visibility = Visibility.Collapsed;
            await ReloadAsync();
        });
    }
}
