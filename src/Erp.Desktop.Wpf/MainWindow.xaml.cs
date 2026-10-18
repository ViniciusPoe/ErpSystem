using System.Windows;
using Erp.Application;
using Erp.Desktop.Wpf.Diagnostics;
using Erp.Desktop.Wpf.Views;
using Erp.Infrastructure.MySql.Config;
using Erp.Infrastructure.MySql.Data;
using Erp.Infrastructure.MySql.Repositories;

namespace Erp.Desktop.Wpf;

public partial class MainWindow : Window
{
    private readonly SalesApplicationService _salesService;
    private readonly InventoryApplicationService _inventoryService;
    private readonly FinanceApplicationService _financeService;

    public MainWindow()
    {
        InitializeComponent();

        var connectionString = MySqlSettings.GetConnectionString();
        var connectionFactory = new MySqlConnectionFactory(connectionString);

        var salesRepository = new MySqlSalesRepository(connectionFactory);
        _salesService = new SalesApplicationService(salesRepository);

        var inventoryRepository = new MySqlInventoryRepository(connectionFactory);
        _inventoryService = new InventoryApplicationService(inventoryRepository);

        var financeRepository = new MySqlFinanceRepository(connectionFactory);
        _financeService = new FinanceApplicationService(financeRepository);

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            TerminalDebug.Info("Inicializando modulo de vendas e estoque...");
            await _salesService.InitializeAsync();
            OpenCustomers();
        }
        catch (Exception ex)
        {
            TerminalDebug.Error("Falha ao inicializar modulo", ex);
            MessageBox.Show("Falha ao inicializar banco de dados. Veja o terminal para detalhes.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CustomersMenu_Click(object sender, RoutedEventArgs e) => OpenCustomers();
    private void ProductsMenu_Click(object sender, RoutedEventArgs e) => OpenProducts();
    private void CarriersMenu_Click(object sender, RoutedEventArgs e) => OpenCarriers();
    private void SuppliersMenu_Click(object sender, RoutedEventArgs e) => OpenSuppliers();
    private void MunicipalitiesMenu_Click(object sender, RoutedEventArgs e) => OpenMunicipalities();
    private void ProductClassificationsMenu_Click(object sender, RoutedEventArgs e) => OpenProductClassifications();
    private void ProductBrandsMenu_Click(object sender, RoutedEventArgs e) => OpenProductBrands();

    private void QuotesMenu_Click(object sender, RoutedEventArgs e) => OpenQuotes();
    private void OrdersMenu_Click(object sender, RoutedEventArgs e) => OpenOrders();
    private void InvoicesMenu_Click(object sender, RoutedEventArgs e) => OpenInvoices();

    private void PurchaseRequestsMenu_Click(object sender, RoutedEventArgs e) => OpenPurchaseRequests();
    private void PurchaseOrdersMenu_Click(object sender, RoutedEventArgs e) => OpenPurchaseOrders();
    private void PurchasesMenu_Click(object sender, RoutedEventArgs e) => OpenPurchases();
    private void AccountsReceivableMenu_Click(object sender, RoutedEventArgs e) => OpenAccountsReceivable();
    private void AccountsPayableMenu_Click(object sender, RoutedEventArgs e) => OpenAccountsPayable();

    private void StockBalancesMenu_Click(object sender, RoutedEventArgs e) => OpenStockBalances();
    private void StockEntriesMenu_Click(object sender, RoutedEventArgs e) => OpenStockEntries();
    private void StockOutputsMenu_Click(object sender, RoutedEventArgs e) => OpenStockOutputs();

    private void OpenCustomers() => ScreenHost.Content = new CustomerCatalogView(_salesService);
    private void OpenProducts() => ScreenHost.Content = new ProductCatalogView(_salesService);
    private void OpenCarriers() => ScreenHost.Content = new CarrierCatalogView(_salesService);
    private void OpenSuppliers() => ScreenHost.Content = new SupplierCatalogView(_inventoryService, _salesService);
    private void OpenMunicipalities() => ScreenHost.Content = new MunicipalityCatalogView(_salesService);
    private void OpenProductClassifications() => ScreenHost.Content = new ProductClassificationView(_salesService);
    private void OpenProductBrands() => ScreenHost.Content = new ProductBrandView(_salesService);

    private void OpenQuotes() => ScreenHost.Content = new QuotesView(_salesService);
    private void OpenOrders() => ScreenHost.Content = new OrdersView(_salesService);
    private void OpenInvoices() => ScreenHost.Content = new InvoicesView(_salesService);

    private void OpenPurchaseRequests() => ScreenHost.Content = new PurchaseRequestsView(_inventoryService, _salesService);
    private void OpenPurchaseOrders() => ScreenHost.Content = new PurchaseOrdersView(_inventoryService, _salesService);
    private void OpenPurchases() => ScreenHost.Content = new PurchasesView(_inventoryService);
    private void OpenAccountsReceivable() => ScreenHost.Content = new AccountsReceivableView(_financeService, _salesService);
    private void OpenAccountsPayable() => ScreenHost.Content = new AccountsPayableView(_financeService, _inventoryService);

    private void OpenStockBalances() => ScreenHost.Content = new StockBalancesView(_inventoryService);
    private void OpenStockEntries() => ScreenHost.Content = new StockEntriesView(_inventoryService, _salesService);
    private void OpenStockOutputs() => ScreenHost.Content = new StockOutputsView(_inventoryService, _salesService);
}
