using System.Windows;
using System.Windows.Controls;
using Erp.Application;

namespace Erp.Desktop.Wpf.Views;

public partial class StockBalancesView : UserControl
{
    private readonly InventoryApplicationService _service;

    public StockBalancesView(InventoryApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Carregar saldo de estoque", async () =>
        {
            BalancesGrid.ItemsSource = await _service.GetProductBalancesAsync();
        });
    }
}
