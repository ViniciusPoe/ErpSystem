using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class InvoicesView : UserControl
{
    private readonly SalesApplicationService _service;
    private readonly List<SalesItemDraft> _draftItems = [];
    private int _selectedOrderId;
    private InvoiceSummary? _selectedInvoice;

    public InvoicesView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar notas fiscais", async () =>
        {
            var orders = await _service.GetOrdersAsync();
            ReadyOrdersGrid.ItemsSource = orders.Where(o => o.Status == SalesStatuses.OrderReadyForInvoice).ToList();
            InvoicesGrid.ItemsSource = await _service.GetInvoicesAsync();

            CustomerComboBox.ItemsSource = await _service.GetCustomersAsync();
            CarrierComboBox.ItemsSource = await _service.GetCarriersAsync();
            ProductComboBox.ItemsSource = await _service.GetProductsAsync();

            _selectedOrderId = 0;
            _selectedInvoice = null;
            EditButton.IsEnabled = false;
            NewButton.IsEnabled = true;
            GeneratePanel.Visibility = Visibility.Collapsed;
            PrintPanel.Visibility = Visibility.Collapsed;
            FormPanel.Visibility = Visibility.Collapsed;
            InvoiceActionHintTextBlock.Text = "Selecione um pedido liberado e clique em Editar para gerar a NF.";
            ResetDraft();
        });
    }

    private void ReadyOrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReadyOrdersGrid.SelectedItem is OrderSummary order)
        {
            _selectedOrderId = order.Id;
            _selectedInvoice = null;
            InvoicesGrid.SelectedItem = null;
            EditButton.IsEnabled = true;
            InvoiceActionHintTextBlock.Text = $"Pedido #{order.Number} - {order.CustomerName} selecionado.";
            return;
        }

        _selectedOrderId = 0;
        EditButton.IsEnabled = _selectedInvoice is not null;
        InvoiceActionHintTextBlock.Text = "Selecione um pedido liberado e clique em Editar para gerar a NF.";
    }

    private void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedInvoice = InvoicesGrid.SelectedItem as InvoiceSummary;
        if (_selectedInvoice is null)
        {
            PrintPanel.Visibility = Visibility.Collapsed;
            PrintInvoiceButton.IsEnabled = false;
            EditButton.IsEnabled = _selectedOrderId > 0;
            return;
        }

        _selectedOrderId = 0;
        ReadyOrdersGrid.SelectedItem = null;
        EditButton.IsEnabled = true;
        GeneratePanel.Visibility = Visibility.Collapsed;
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        ReadyOrdersGrid.SelectedItem = null;
        InvoicesGrid.SelectedItem = null;
        _selectedOrderId = 0;
        _selectedInvoice = null;
        GeneratePanel.Visibility = Visibility.Collapsed;
        PrintPanel.Visibility = Visibility.Collapsed;
        FormPanel.Visibility = Visibility.Visible;
        ResetDraft();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;

        if (_selectedOrderId > 0)
        {
            GeneratePanel.Visibility = Visibility.Visible;
            PrintPanel.Visibility = Visibility.Collapsed;
            GenerateInvoiceButton.IsEnabled = true;
            return;
        }

        if (_selectedInvoice is not null)
        {
            PrintPanel.Visibility = Visibility.Visible;
            GeneratePanel.Visibility = Visibility.Collapsed;
            PrintInvoiceButton.IsEnabled = true;
        }
    }

    private void CancelGenerateButton_Click(object sender, RoutedEventArgs e)
    {
        GeneratePanel.Visibility = Visibility.Collapsed;
    }

    private async void GenerateInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedOrderId <= 0) return;

        await ViewHelper.RunSafeAsync("Gerar nota fiscal", async () =>
        {
            GenerateInvoiceButton.IsEnabled = false;
            await _service.GenerateInvoiceAsync(_selectedOrderId);
            await ReloadAsync();
        });
    }

    private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductComboBox.SelectedItem is Product p)
            UnitPriceTextBox.Text = p.UnitPrice.ToString("N2", CultureInfo.CurrentCulture);
    }

    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        ViewHelper.RunSafe("Adicionar item na nota", () =>
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

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        ResetDraft();
    }

    private async void GenerateDirectInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Gerar nota fiscal direta", async () =>
        {
            if (CustomerComboBox.SelectedItem is not Customer customer || _draftItems.Count == 0) return;

            var draft = new OrderDraft
            {
                CustomerId = customer.Id,
                CarrierId = (CarrierComboBox.SelectedItem as Carrier)?.Id,
                Items = _draftItems.ToList(),
                DiscountAmount = ComputeDiscountAmount(),
                FreightAmount = ParseFreight(),
                FreightType = (FreightTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "CIF",
                PaymentTerm = ViewHelper.PaymentTermFromComboBox(PaymentTermComboBox)
            };

            await _service.CreateDirectInvoiceAsync(draft);
            FormPanel.Visibility = Visibility.Collapsed;
            await ReloadAsync();
        });
    }

    private void PrintInvoiceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedInvoice is null) return;
        ViewHelper.RunSafe("Imprimir PDF", () => PdfHelper.GenerateAndOpenInvoicePdf(_selectedInvoice));
    }

    private decimal ComputeDiscountAmount()
    {
        var subtotal = _draftItems.Sum(i => i.Quantity * i.UnitPrice);
        ViewHelper.TryDecimal(DiscountTextBox.Text, out var pct);
        return decimal.Round(subtotal * pct / 100, 2);
    }

    private decimal ParseFreight()
    {
        ViewHelper.TryDecimal(FreightTextBox.Text, out var freight);
        return freight;
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

        var totals = _service.CalculateTotals(_draftItems, ComputeDiscountAmount(), ParseFreight());
        SubtotalTextBlock.Text = totals.Subtotal.ToString("N2", CultureInfo.CurrentCulture);
        TotalTextBlock.Text = totals.GrandTotal.ToString("N2", CultureInfo.CurrentCulture);
    }

    private void ResetDraft()
    {
        _draftItems.Clear();
        DiscountTextBox.Text = "0";
        FreightTextBox.Text = "0";
        PaymentTermComboBox.SelectedIndex = 0;
        FreightTypeComboBox.SelectedIndex = 0;
        QuantityTextBox.Clear();
        UnitPriceTextBox.Clear();
        ProductComboBox.SelectedItem = null;
        CarrierComboBox.SelectedItem = null;
        CustomerComboBox.SelectedItem = null;
        RefreshDraft();
    }
}
