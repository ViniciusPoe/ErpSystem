using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class ProductBrandView : UserControl
{
    private readonly SalesApplicationService _service;
    private int _selectedId;

    public ProductBrandView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += async (_, _) => await LoadBrandsAsync();
    }

    private async Task LoadBrandsAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar marcas", async () =>
        {
            BrandsGrid.ItemsSource = await _service.GetBrandsAsync();
        });
    }

    private void BrandsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BrandsGrid.SelectedItem is not ProductBrand b)
        {
            _selectedId = 0;
            UpdateButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            return;
        }

        _selectedId = b.Id;
        CodeTextBox.Text = b.Code;
        NameTextBox.Text = b.Name;
        UpdateButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
    }

    private async void AddBrand_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeTextBox.Text) || string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Preencha Codigo e Nome.", "Marca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ViewHelper.RunSafeAsync("Adicionar marca", async () =>
        {
            await _service.AddBrandAsync(new ProductBrand { Code = CodeTextBox.Text.Trim(), Name = NameTextBox.Text.Trim() });
            CodeTextBox.Clear();
            NameTextBox.Clear();
            await LoadBrandsAsync();
        });
    }

    private async void UpdateBrand_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0) return;
        await ViewHelper.RunSafeAsync("Atualizar marca", async () =>
        {
            await _service.UpdateBrandAsync(new ProductBrand { Id = _selectedId, Code = CodeTextBox.Text.Trim(), Name = NameTextBox.Text.Trim() });
            await LoadBrandsAsync();
        });
    }

    private async void DeleteBrand_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0 || !ViewHelper.Confirm("Excluir esta marca?")) return;
        await ViewHelper.RunSafeAsync("Excluir marca", async () =>
        {
            await _service.DeleteBrandAsync(_selectedId);
            _selectedId = 0;
            CodeTextBox.Clear();
            NameTextBox.Clear();
            await LoadBrandsAsync();
        });
    }
}
