using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Inventory;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class PurchaseOrdersView : UserControl
{
    private readonly InventoryApplicationService _inventoryService;
    private readonly SalesApplicationService _salesService;
    private int _selectedRequestId;
    private bool _editingRequest;
    private Dictionary<string, int> _productBySku = [];

    public PurchaseOrdersView(InventoryApplicationService inventoryService, SalesApplicationService salesService)
    {
        InitializeComponent();
        _inventoryService = inventoryService;
        _salesService = salesService;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar ordens de compra", async () =>
        {
            var suppliers = await _inventoryService.GetSuppliersAsync();
            SupplierComboBox.ItemsSource = suppliers;

            var products = await _salesService.GetProductsAsync();
            ProductComboBox.ItemsSource = products;
            _productBySku = products.ToDictionary(p => p.Sku, p => p.Id);

            var requests = await _inventoryService.GetPurchaseRequestsAsync();
            var purchases = await _inventoryService.GetPurchasesAsync();
            var purchaseByRequestId = purchases
                .Where(p => p.RequestId.HasValue)
                .ToDictionary(p => p.RequestId!.Value);

            var rows = new List<PurchaseOrderRow>();

            foreach (var p in purchases)
            {
                var requestLabel = p.RequestId.HasValue ? p.RequestId.Value.ToString() : "-";
                rows.Add(new PurchaseOrderRow
                {
                    SourceLabel = p.SourceType == "SOLICITACAO" ? "Solicitacao" : "Direta",
                    RequestLabel = requestLabel,
                    RequestId = p.RequestId,
                    ProductSku = p.ProductSku,
                    SupplierName = p.SupplierName,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice,
                    Total = p.Quantity * p.UnitPrice,
                    ApprovalStatus = p.SourceType == "SOLICITACAO" ? "APROVADA" : "DIRETA",
                    DeliveryStatus = p.Status switch
                    {
                        "PENDENTE_ENTREGA" => "PENDENTE",
                        "ENTREGUE" => "ENTREGUE",
                        "CANCELADA" => "CANCELADA",
                        _ => p.Status
                    },
                    DisplayStatus = p.Status switch
                    {
                        "ENTREGUE" => "ENTREGUE",
                        "CANCELADA" => "CANCELADA",
                        _ => "AGUARDANDO_ENTREGA"
                    },
                    IsOpenRequest = false
                });
            }

            foreach (var r in requests.Where(x => x.Status == "ABERTA"))
            {
                if (purchaseByRequestId.ContainsKey(r.Id))
                    continue;

                rows.Add(new PurchaseOrderRow
                {
                    SourceLabel = "Solicitacao",
                    RequestLabel = r.Id.ToString(),
                    RequestId = r.Id,
                    ProductSku = r.ProductSku,
                    SupplierName = r.SupplierName ?? "-",
                    Quantity = r.RequestedQuantity,
                    UnitPrice = 0,
                    Total = 0,
                    ApprovalStatus = "PENDENTE",
                    DeliveryStatus = "-",
                    DisplayStatus = "PENDENTE",
                    IsOpenRequest = true
                });
            }

            UnifiedGrid.ItemsSource = rows.OrderByDescending(r => r.RequestId ?? int.MaxValue).ThenBy(r => r.SourceLabel).ToList();
            ResetScreen();
        });
    }

    private void UnifiedGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        if (UnifiedGrid.SelectedItem is not PurchaseOrderRow row)
        {
            _selectedRequestId = 0;
            EditButton.IsEnabled = false;
            return;
        }

        _selectedRequestId = row.IsOpenRequest ? row.RequestId ?? 0 : 0;
        EditButton.IsEnabled = row.IsOpenRequest;
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        _editingRequest = false;
        _selectedRequestId = 0;
        FormTitle.Text = "Nova Compra Direta";
        RequestHintText.Visibility = Visibility.Collapsed;
        ProductComboBox.IsEnabled = true;
        RejectRequestButton.Visibility = Visibility.Collapsed;
        SaveButton.Content = "Gerar Compra";
        UnifiedGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
        ClearForm();
        FormPanel.Visibility = Visibility.Visible;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (UnifiedGrid.SelectedItem is not PurchaseOrderRow row || !row.IsOpenRequest || !row.RequestId.HasValue)
            return;

        _editingRequest = true;
        _selectedRequestId = row.RequestId.Value;
        FormTitle.Text = "Aprovar Solicitacao";
        RequestHintText.Text = $"Solicitacao #{row.RequestId.Value} - produto {row.ProductSku}";
        RequestHintText.Visibility = Visibility.Visible;
        ProductComboBox.IsEnabled = false;
        SelectProductBySku(row.ProductSku);
        QtyTextBox.Text = row.Quantity.ToString("N2", CultureInfo.CurrentCulture);
        UnitPriceTextBox.Text = "0,00";
        RejectRequestButton.Visibility = Visibility.Visible;
        SaveButton.Content = "Aprovar e Gerar Compra";
        FormPanel.Visibility = Visibility.Visible;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Salvar compra", async () =>
        {
            if (ProductComboBox.SelectedItem is not Product product) return;
            if (SupplierComboBox.SelectedItem is not Supplier supplier) return;
            if (!ViewHelper.TryDecimal(QtyTextBox.Text, out var qty) || qty <= 0) return;
            if (!ViewHelper.TryDecimal(UnitPriceTextBox.Text, out var price) || price < 0) return;
            var paymentTerm = ViewHelper.PaymentTermFromComboBox(PaymentTermComboBox);
            var notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

            if (_editingRequest && _selectedRequestId > 0)
            {
                await _inventoryService.ApprovePurchaseRequestAsync(_selectedRequestId, supplier.Id, qty, price, paymentTerm);
            }
            else
            {
                await _inventoryService.CreateDirectPurchaseAsync(product.Id, supplier.Id, qty, price, paymentTerm, notes);
            }

            FormPanel.Visibility = Visibility.Collapsed;
            await ReloadAsync();
        });
    }

    private async void RejectRequestButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRequestId <= 0) return;
        if (!ViewHelper.Confirm("Confirma a recusa desta solicitacao de compra?", "Recusar Solicitacao")) return;

        await ViewHelper.RunSafeAsync("Recusar solicitacao", async () =>
        {
            await _inventoryService.RejectPurchaseRequestAsync(_selectedRequestId);
            FormPanel.Visibility = Visibility.Collapsed;
            await ReloadAsync();
        });
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        ClearForm();
    }

    private void ClearForm()
    {
        ProductComboBox.SelectedItem = null;
        SupplierComboBox.SelectedItem = null;
        QtyTextBox.Text = "0,00";
        UnitPriceTextBox.Text = "0,00";
        NotesTextBox.Clear();
        PaymentTermComboBox.SelectedIndex = 0;
        TotalTextBlock.Text = "0,00";
    }

    private void ResetScreen()
    {
        _selectedRequestId = 0;
        _editingRequest = false;
        EditButton.IsEnabled = false;
        FormPanel.Visibility = Visibility.Collapsed;
        ClearForm();
    }

    private void SelectProductBySku(string sku)
    {
        if (!_productBySku.ContainsKey(sku)) return;
        foreach (var item in ProductComboBox.Items)
        {
            if (item is Product p && p.Sku == sku)
            {
                ProductComboBox.SelectedItem = item;
                return;
            }
        }
    }
}

internal sealed class PurchaseOrderRow
{
    public required string SourceLabel { get; init; }
    public required string RequestLabel { get; init; }
    public int? RequestId { get; init; }
    public required string ProductSku { get; init; }
    public required string SupplierName { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Total { get; init; }
    public required string ApprovalStatus { get; init; }
    public required string DeliveryStatus { get; init; }
    public required string DisplayStatus { get; init; }
    public bool IsOpenRequest { get; init; }
}
