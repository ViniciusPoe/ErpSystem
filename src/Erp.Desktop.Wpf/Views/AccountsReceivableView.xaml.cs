using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Finance;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class AccountsReceivableView : UserControl
{
    private readonly FinanceApplicationService _financeService;
    private readonly SalesApplicationService _salesService;
    private int _selectedId;

    public AccountsReceivableView(FinanceApplicationService financeService, SalesApplicationService salesService)
    {
        InitializeComponent();
        _financeService = financeService;
        _salesService = salesService;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar contas a receber", async () =>
        {
            CustomerComboBox.ItemsSource = await _salesService.GetCustomersAsync();
            DueDatePicker.SelectedDate = DateTime.Today;
            ReceivablesGrid.ItemsSource = await _financeService.GetAccountsReceivableAsync();
            _selectedId = 0;
            EditButton.IsEnabled = false;
            FormPanel.Visibility = Visibility.Collapsed;
            EditActionsPanel.Visibility = Visibility.Collapsed;
        });
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        ReceivablesGrid.SelectedItem = null;
        _selectedId = 0;
        EditButton.IsEnabled = false;
        EditActionsPanel.Visibility = Visibility.Collapsed;
        FormPanel.Visibility = Visibility.Visible;
        CustomerComboBox.SelectedItem = null;
        AmountTextBox.Clear();
        PaymentTermComboBox.SelectedIndex = 0;
        DueDatePicker.SelectedDate = DateTime.Today;
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
    }

    private async void SaveReceivableButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Salvar conta a receber", async () =>
        {
            if (!ViewHelper.TryDecimal(AmountTextBox.Text, out var totalAmount) || totalAmount <= 0) return;
            var customerId = (CustomerComboBox.SelectedItem as Customer)?.Id;
            var paymentTerm = ViewHelper.PaymentTermFromComboBox(PaymentTermComboBox);
            var dueBase = DueDatePicker.SelectedDate ?? DateTime.Today;

            await _financeService.CreateManualReceivableAsync(customerId, totalAmount, paymentTerm, dueBase);
            FormPanel.Visibility = Visibility.Collapsed;
            await ReloadAsync();
        });
    }

    private void ReceivablesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedId = ReceivablesGrid.SelectedItem is AccountReceivableSummary item ? item.Id : 0;
        EditButton.IsEnabled = _selectedId > 0;
        EditActionsPanel.Visibility = Visibility.Collapsed;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0) return;
        FormPanel.Visibility = Visibility.Collapsed;
        EditActionsPanel.Visibility = Visibility.Visible;
    }

    private async void MarkPaidButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0) return;
        await ViewHelper.RunSafeAsync("Marcar recebimento", async () =>
        {
            await _financeService.MarkReceivablePaidAsync(_selectedId);
            await ReloadAsync();
        });
    }
}
