using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Inventory;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class StockOutputsView : UserControl
{
    private readonly InventoryApplicationService _inventoryService;
    private readonly SalesApplicationService _salesService;
    private int _selectedOutputId;

    public StockOutputsView(InventoryApplicationService inventoryService, SalesApplicationService salesService)
    {
        InitializeComponent();
        _inventoryService = inventoryService;
        _salesService = salesService;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar saidas de estoque", async () =>
        {
            ProductComboBox.ItemsSource = await _salesService.GetProductsAsync();
            OutputsGrid.ItemsSource = await _inventoryService.GetStockOutputsAsync();
            _selectedOutputId = 0;
            EditButton.IsEnabled = false;
            FormPanel.Visibility = Visibility.Collapsed;
        });
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        OutputsGrid.SelectedItem = null;
        _selectedOutputId = 0;
        EditButton.IsEnabled = false;
        ProductComboBox.SelectedItem = null;
        QuantityTextBox.Clear();
        NotesTextBox.Clear();
        FormPanel.Visibility = Visibility.Visible;
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Salvar saida manual", async () =>
        {
            if (ProductComboBox.SelectedItem is not Product product) return;
            if (!ViewHelper.TryDecimal(QuantityTextBox.Text, out var qty) || qty <= 0) return;
            var notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
            await _inventoryService.CreateStockOutputAsync(product.Id, qty, notes);
            FormPanel.Visibility = Visibility.Collapsed;
            await ReloadAsync();
        });
    }

    private void OutputsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedOutputId = OutputsGrid.SelectedItem is StockMovementSummary item ? item.Id : 0;
        EditButton.IsEnabled = _selectedOutputId > 0;
        FormPanel.Visibility = Visibility.Collapsed;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOutputId <= 0) return;
        MessageBox.Show("Movimentos de saida ja cadastrados nao podem ser editados manualmente.",
            "Estoque - Saidas", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
