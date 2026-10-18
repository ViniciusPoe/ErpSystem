using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class QuotesView : UserControl
{
    private readonly SalesApplicationService _service;
    private readonly List<SalesItemDraft> _draftItems = [];
    private int _editingQuoteId;

    public QuotesView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAllAsync();

    private async Task ReloadAllAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar orcamentos", async () =>
        {
            CustomerComboBox.ItemsSource = await _service.GetCustomersAsync();
            ProductComboBox.ItemsSource = await _service.GetProductsAsync();
            QuotesGrid.ItemsSource = await _service.GetQuotesAsync();
            EditButton.IsEnabled = false;
            FormPanel.Visibility = Visibility.Collapsed;
            _editingQuoteId = 0;
        });
    }

    private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductComboBox.SelectedItem is Product p)
            UnitPriceTextBox.Text = p.UnitPrice.ToString("N2", CultureInfo.CurrentCulture);
    }

    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        ViewHelper.RunSafe("Adicionar item", () =>
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

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        QuotesGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
        FormTitle.Text = "Novo Orcamento";
        FormPanel.Visibility = Visibility.Visible;
        ResetDraft();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (QuotesGrid.SelectedItem is not QuoteSummary s) return;
        FormTitle.Text = $"Editar Orcamento #{s.Number}";
        FormPanel.Visibility = Visibility.Visible;
        _ = LoadQuoteAsync(s);
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        ResetDraft();
    }

    private async void SaveQuoteButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Salvar orcamento", async () =>
        {
            if (CustomerComboBox.SelectedItem is not Customer c || _draftItems.Count == 0) return;

            var discountAmt = ComputeDiscountAmount();
            var draft = new QuoteDraft
            {
                CustomerId = c.Id,
                Items = _draftItems.ToList(),
                DiscountAmount = discountAmt
            };

            if (_editingQuoteId > 0)
            {
                await _service.UpdateQuoteAsync(_editingQuoteId, draft);
            }
            else
            {
                await _service.CreateQuoteAsync(draft);
            }

            FormPanel.Visibility = Visibility.Collapsed;
            ResetDraft();
            await ReloadAllAsync();
        });
    }

    private async void QuotesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QuotesGrid.SelectedItem is not QuoteSummary s)
        {
            _editingQuoteId = 0;
            EditButton.IsEnabled = false;
            return;
        }

        EditButton.IsEnabled = true;
        if (FormPanel.Visibility != Visibility.Visible) return;

        await LoadQuoteAsync(s);
    }

    private void ResetDraft()
    {
        _editingQuoteId = 0;
        _draftItems.Clear();
        DiscountTextBox.Text = "0";
        QuantityTextBox.Clear();
        UnitPriceTextBox.Clear();
        ProductComboBox.SelectedItem = null;
        RefreshDraft();
    }

    private async Task LoadQuoteAsync(QuoteSummary summary)
    {
        await ViewHelper.RunSafeAsync("Carregar orcamento", async () =>
        {
            var q = await _service.GetQuoteAsync(summary.Id);
            if (q is null) return;
            _editingQuoteId = q.Id;

            foreach (var item in CustomerComboBox.Items)
                if (item is Customer c && c.Id == q.CustomerId) { CustomerComboBox.SelectedItem = item; break; }

            _draftItems.Clear();
            _draftItems.AddRange(q.Items);

            var subtotal = _draftItems.Sum(i => i.Quantity * i.UnitPrice);
            var pct = subtotal > 0 ? Math.Round(q.DiscountAmount / subtotal * 100, 2) : 0;
            DiscountTextBox.Text = pct.ToString("N2", CultureInfo.CurrentCulture);

            RefreshDraft();
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
        var totals = _service.CalculateTotals(_draftItems, discountAmt, 0);
        SubtotalTextBlock.Text = totals.Subtotal.ToString("N2", CultureInfo.CurrentCulture);
        TotalTextBlock.Text = totals.GrandTotal.ToString("N2", CultureInfo.CurrentCulture);
    }
}
