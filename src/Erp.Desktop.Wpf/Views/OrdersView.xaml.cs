using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class OrdersView : UserControl
{
    private readonly SalesApplicationService _service;
    private readonly List<SalesItemDraft> _draftItems = [];
    private int _selectedOrderId;
    private int _selectedQuoteId;

    public OrdersView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAllAsync();

    private async Task ReloadAllAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar pedidos", async () =>
        {
            var carriers = await _service.GetCarriersAsync();
            CustomerComboBox.ItemsSource = await _service.GetCustomersAsync();
            CarrierComboBox.ItemsSource = carriers;
            ApprovalCarrierComboBox.ItemsSource = carriers;
            ProductComboBox.ItemsSource = await _service.GetProductsAsync();

            var quotes = await _service.GetQuotesAsync();
            var orders = await _service.GetOrdersAsync();
            var rows = new List<OrderOrQuoteRow>();

            foreach (var q in quotes)
            {
                if (string.Equals(q.Status, SalesStatuses.QuoteApproved, StringComparison.OrdinalIgnoreCase))
                    continue;

                var statusDisplay = q.Status switch
                {
                    SalesStatuses.QuoteOpen => "Aguardando Aprovacao",
                    SalesStatuses.QuoteRejected => "Reprovado",
                    _ => q.Status
                };

                var internalStatus = q.Status switch
                {
                    SalesStatuses.QuoteOpen => "ABERTO_ORC",
                    SalesStatuses.QuoteRejected => "REPROVADO_ORC",
                    _ => q.Status
                };

                rows.Add(new OrderOrQuoteRow
                {
                    Id = q.Id,
                    Number = q.Number,
                    CustomerName = q.CustomerName,
                    TotalAmount = q.TotalAmount,
                    Status = internalStatus,
                    StatusDisplay = statusDisplay,
                    Tipo = "Orcamento",
                    FreightType = "",
                    PaymentTerm = "",
                    SourceQuoteNumber = "",
                    CreatedAt = q.CreatedAt,
                    IsQuote = true
                });
            }

            foreach (var o in orders)
            {
                var statusDisplay = o.Status switch
                {
                    SalesStatuses.OrderOpen => "Aberto",
                    SalesStatuses.OrderReadyForInvoice => "Liberado NF",
                    SalesStatuses.OrderInvoiced => "Faturado",
                    _ => o.Status
                };

                rows.Add(new OrderOrQuoteRow
                {
                    Id = o.Id,
                    Number = o.Number,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    StatusDisplay = statusDisplay,
                    Tipo = "Pedido",
                    FreightType = o.FreightType,
                    PaymentTerm = o.PaymentTerm,
                    SourceQuoteNumber = o.SourceQuoteNumber.HasValue ? o.SourceQuoteNumber.Value.ToString() : "",
                    CreatedAt = o.CreatedAt,
                    IsQuote = false
                });
            }

            OrdersGrid.ItemsSource = rows;

            _selectedQuoteId = 0;
            _selectedOrderId = 0;
            EditButton.IsEnabled = false;
            FormPanel.Visibility = Visibility.Collapsed;
            ApprovalPanel.Visibility = Visibility.Collapsed;
            OrderActionsPanel.Visibility = Visibility.Collapsed;
        });
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedQuoteId = 0;
        _selectedOrderId = 0;
        ApprovalPanel.Visibility = Visibility.Collapsed;
        OrderActionsPanel.Visibility = Visibility.Collapsed;

        if (OrdersGrid.SelectedItem is not OrderOrQuoteRow row)
        {
            EditButton.IsEnabled = false;
            return;
        }

        if (row.IsQuote)
        {
            var canEditQuote = string.Equals(row.Status, "ABERTO_ORC", StringComparison.OrdinalIgnoreCase);
            _selectedQuoteId = canEditQuote ? row.Id : 0;
            EditButton.IsEnabled = canEditQuote;
            return;
        }

        _selectedOrderId = row.Id;
        EditButton.IsEnabled = true;
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        ApprovalPanel.Visibility = Visibility.Collapsed;
        OrderActionsPanel.Visibility = Visibility.Collapsed;
        OrdersGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
        FormTitle.Text = "Novo Pedido";
        FormPanel.Visibility = Visibility.Visible;
        ResetOrderDraft();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not OrderOrQuoteRow row)
            return;

        FormPanel.Visibility = Visibility.Collapsed;
        ApprovalPanel.Visibility = Visibility.Collapsed;
        OrderActionsPanel.Visibility = Visibility.Collapsed;

        if (row.IsQuote)
        {
            if (!string.Equals(row.Status, "ABERTO_ORC", StringComparison.OrdinalIgnoreCase))
                return;

            _selectedQuoteId = row.Id;
            ApprovalPanel.Visibility = Visibility.Visible;
            return;
        }

        _selectedOrderId = row.Id;
        var canApprove = string.Equals(row.Status, SalesStatuses.OrderOpen, StringComparison.OrdinalIgnoreCase);
        ApproveForInvoiceButton.IsEnabled = canApprove;
        OrderActionHintTextBlock.Text = canApprove
            ? "Pedido aberto - pronto para liberacao de nota fiscal."
            : row.Status == SalesStatuses.OrderReadyForInvoice
                ? "Ja liberado para nota fiscal."
                : "Pedido ja faturado.";
        OrderActionsPanel.Visibility = Visibility.Visible;
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        ResetOrderDraft();
    }

    private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductComboBox.SelectedItem is Product p)
            UnitPriceTextBox.Text = p.UnitPrice.ToString("N2", CultureInfo.CurrentCulture);
    }

    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        ViewHelper.RunSafe("Adicionar item no pedido", () =>
        {
            if (ProductComboBox.SelectedItem is not Product product) return;
            if (!ViewHelper.TryDecimal(QuantityTextBox.Text, out var qty) || qty <= 0) return;
            if (!ViewHelper.TryDecimal(UnitPriceTextBox.Text, out var price) || price <= 0) return;

            _draftItems.Add(new SalesItemDraft
            {
                ProductId = product.Id,
                Description = product.Name,
                Quantity = qty,
                UnitPrice = price
            });

            RefreshDraft();
        });
    }

    private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_service is null) return;
        RefreshDraft();
    }

    private void FreightTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_service is null) return;
        RefreshDraft();
    }

    private void ResetOrderDraft()
    {
        _draftItems.Clear();
        DiscountTextBox.Text = "0";
        FreightTextBox.Text = "0";
        PaymentTermComboBox.SelectedIndex = 0;
        FreightTypeComboBox.SelectedIndex = 0;
        QuantityTextBox.Clear();
        UnitPriceTextBox.Clear();
        ProductComboBox.SelectedItem = null;
        RefreshDraft();
    }

    private async void SaveOrderButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Salvar pedido", async () =>
        {
            if (CustomerComboBox.SelectedItem is not Customer customer || _draftItems.Count == 0) return;

            var discountAmt = ComputeDiscountAmount();
            ViewHelper.TryDecimal(FreightTextBox.Text, out var freight);
            var freightType = (FreightTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "CIF";

            var draft = new OrderDraft
            {
                CustomerId = customer.Id,
                CarrierId = (CarrierComboBox.SelectedItem as Carrier)?.Id,
                Items = _draftItems.ToList(),
                DiscountAmount = discountAmt,
                FreightAmount = freight,
                FreightType = freightType,
                PaymentTerm = ViewHelper.PaymentTermFromComboBox(PaymentTermComboBox)
            };

            await _service.CreateOrderAsync(draft);
            FormPanel.Visibility = Visibility.Collapsed;
            ResetOrderDraft();
            await ReloadAllAsync();
        });
    }

    private async void ApproveQuoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedQuoteId <= 0 || OrdersGrid.SelectedItem is not OrderOrQuoteRow row || !row.IsQuote) return;

        ViewHelper.TryDecimal(ApprovalFreightTextBox.Text, out var freight);
        if (freight <= 0)
        {
            MessageBox.Show("Informe o valor do frete para aprovar o orcamento.", "Pedidos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var paymentTerm = ViewHelper.PaymentTermFromComboBox(ApprovalPaymentTermComboBox);
        var carrierId = (ApprovalCarrierComboBox.SelectedItem as Carrier)?.Id;
        var freightType = (ApprovalFreightTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "CIF";

        await ViewHelper.RunSafeAsync("Aprovar orcamento", async () =>
        {
            var orderId = await _service.ApproveQuoteAsync(_selectedQuoteId, paymentTerm, carrierId, freight, freightType);
            MessageBox.Show($"Orcamento #{row.Number} aprovado - Pedido #{orderId} criado.",
                "Pedidos", MessageBoxButton.OK, MessageBoxImage.Information);
            await ReloadAllAsync();
        });
    }

    private async void RejectQuoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedQuoteId <= 0 || OrdersGrid.SelectedItem is not OrderOrQuoteRow row || !row.IsQuote) return;
        if (!ViewHelper.Confirm($"Confirma a reprovacao do Orcamento #{row.Number}?", "Reprovar Orcamento")) return;

        await ViewHelper.RunSafeAsync("Reprovar orcamento", async () =>
        {
            await _service.RejectQuoteAsync(_selectedQuoteId);
            await ReloadAllAsync();
        });
    }

    private async void ApproveForInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrderId <= 0 || OrdersGrid.SelectedItem is not OrderOrQuoteRow row || row.IsQuote) return;

        if (!string.Equals(row.Status, SalesStatuses.OrderOpen, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Este pedido nao pode ser liberado novamente.", "Pedidos", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await ViewHelper.RunSafeAsync("Liberar pedido para nota fiscal", async () =>
        {
            await _service.MarkOrderReadyForInvoiceAsync(_selectedOrderId);
            await ReloadAllAsync();
        });
    }

    private decimal ComputeDiscountAmount()
    {
        var subtotal = _draftItems.Sum(i => i.Quantity * i.UnitPrice);
        ViewHelper.TryDecimal(DiscountTextBox.Text, out var pct);
        return decimal.Round(subtotal * pct / 100, 2);
    }

    private void RefreshDraft()
    {
        DraftItemsGrid.ItemsSource = _draftItems.Select(i => new
        {
            i.Description,
            i.Quantity,
            i.UnitPrice,
            LineTotal = decimal.Round(i.Quantity * i.UnitPrice, 2)
        }).ToList();

        var discountAmt = ComputeDiscountAmount();
        ViewHelper.TryDecimal(FreightTextBox.Text, out var freight);
        var totals = _service.CalculateTotals(_draftItems, discountAmt, freight);
        SubtotalTextBlock.Text = totals.Subtotal.ToString("N2", CultureInfo.CurrentCulture);
        TotalTextBlock.Text = totals.GrandTotal.ToString("N2", CultureInfo.CurrentCulture);
    }
}

internal sealed class OrderOrQuoteRow
{
    public int Id { get; init; }
    public int Number { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
    public required string Status { get; init; }
    public required string StatusDisplay { get; init; }
    public required string Tipo { get; init; }
    public required string FreightType { get; init; }
    public required string PaymentTerm { get; init; }
    public required string SourceQuoteNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsQuote { get; init; }
}
