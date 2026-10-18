using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Finance;
using Erp.Modules.Inventory;

namespace Erp.Desktop.Wpf.Views;

public partial class AccountsPayableView : UserControl
{
    private readonly FinanceApplicationService _financeService;
    private readonly InventoryApplicationService _inventoryService;
    private int _selectedPayableId;
    private int _selectedRecurringId;

    public AccountsPayableView(FinanceApplicationService financeService, InventoryApplicationService inventoryService)
    {
        InitializeComponent();
        _financeService = financeService;
        _inventoryService = inventoryService;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar contas a pagar", async () =>
        {
            SupplierComboBox.ItemsSource = await _inventoryService.GetSuppliersAsync();
            DueDatePicker.SelectedDate = DateTime.Today;
            StartDatePicker.SelectedDate = DateTime.Today;

            PayablesGrid.ItemsSource = await _financeService.GetAccountsPayableAsync();
            RecurringGrid.ItemsSource = await _financeService.GetRecurringPayablesAsync();

            _selectedPayableId = 0;
            _selectedRecurringId = 0;
            EditButton.IsEnabled = false;
            FormPanel.Visibility = Visibility.Collapsed;
            EditActionsPanel.Visibility = Visibility.Collapsed;
            MarkPaidButton.IsEnabled = false;
            CancelRecurringButton.IsEnabled = false;
        });
    }

    private void IsSubscriptionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        var isSub = IsSubscriptionCheckBox.IsChecked == true;
        ManualFields.Visibility = isSub ? Visibility.Collapsed : Visibility.Visible;
        SubscriptionFields.Visibility = isSub ? Visibility.Visible : Visibility.Collapsed;
        AmountLabel.Text = isSub ? "Valor mensal *" : "Valor total *";
    }

    private void PayablesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPayableId = PayablesGrid.SelectedItem is AccountPayableSummary row ? row.Id : 0;
        if (_selectedPayableId > 0)
            RecurringGrid.SelectedItem = null;

        _selectedRecurringId = 0;
        EditButton.IsEnabled = _selectedPayableId > 0;
        EditActionsPanel.Visibility = Visibility.Collapsed;
    }

    private void RecurringGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedRecurringId = RecurringGrid.SelectedItem is RecurringPayableSummary row ? row.Id : 0;
        if (_selectedRecurringId > 0)
            PayablesGrid.SelectedItem = null;

        _selectedPayableId = 0;
        EditButton.IsEnabled = _selectedRecurringId > 0;
        EditActionsPanel.Visibility = Visibility.Collapsed;
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Visible;
        EditActionsPanel.Visibility = Visibility.Collapsed;
        EditButton.IsEnabled = false;
        PayablesGrid.SelectedItem = null;
        RecurringGrid.SelectedItem = null;
        _selectedPayableId = 0;
        _selectedRecurringId = 0;
        ClearForm();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPayableId <= 0 && _selectedRecurringId <= 0) return;
        FormPanel.Visibility = Visibility.Collapsed;
        EditActionsPanel.Visibility = Visibility.Visible;
        MarkPaidButton.IsEnabled = _selectedPayableId > 0;
        CancelRecurringButton.IsEnabled = _selectedRecurringId > 0;
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        ClearForm();
    }

    private async void SavePayableButton_Click(object sender, RoutedEventArgs e)
    {
        var isSub = IsSubscriptionCheckBox.IsChecked == true;

        await ViewHelper.RunSafeAsync("Salvar lancamento", async () =>
        {
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text)) return;
            if (!ViewHelper.TryDecimal(AmountTextBox.Text, out var amount) || amount <= 0) return;

            var supplierId = (SupplierComboBox.SelectedItem as Supplier)?.Id;

            if (isSub)
            {
                if (!int.TryParse(MonthsTextBox.Text, out var months) || months <= 0) months = 12;
                var startDate = StartDatePicker.SelectedDate ?? DateTime.Today;
                await _financeService.CreateRecurringPayableAsync(DescriptionTextBox.Text.Trim(), supplierId, amount, startDate, months);
            }
            else
            {
                var paymentTerm = ViewHelper.PaymentTermFromComboBox(PaymentTermComboBox);
                var dueBase = DueDatePicker.SelectedDate ?? DateTime.Today;
                await _financeService.CreateManualPayableAsync(DescriptionTextBox.Text.Trim(), supplierId, amount, paymentTerm, dueBase);
            }

            FormPanel.Visibility = Visibility.Collapsed;
            ClearForm();
            await ReloadAsync();
        });
    }

    private async void CancelRecurringButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRecurringId <= 0) return;
        if (!ViewHelper.Confirm("Confirma o cancelamento desta assinatura?\nAs parcelas futuras nao serao mais geradas.", "Cancelar Assinatura")) return;

        await ViewHelper.RunSafeAsync("Cancelar assinatura", async () =>
        {
            await _financeService.CancelRecurringPayableAsync(_selectedRecurringId);
            _selectedRecurringId = 0;
            await ReloadAsync();
        });
    }

    private async void MarkPaidButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPayableId <= 0) return;
        await ViewHelper.RunSafeAsync("Marcar conta paga", async () =>
        {
            await _financeService.MarkPayablePaidAsync(_selectedPayableId);
            _selectedPayableId = 0;
            await ReloadAsync();
        });
    }

    private void ClearForm()
    {
        DescriptionTextBox.Clear();
        AmountTextBox.Clear();
        MonthsTextBox.Text = "12";
        IsSubscriptionCheckBox.IsChecked = false;
        SupplierComboBox.SelectedItem = null;
        PaymentTermComboBox.SelectedIndex = 0;
        DueDatePicker.SelectedDate = DateTime.Today;
        StartDatePicker.SelectedDate = DateTime.Today;
    }
}
