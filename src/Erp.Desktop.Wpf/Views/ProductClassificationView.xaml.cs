using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class ProductClassificationView : UserControl
{
    private readonly SalesApplicationService _service;
    private int _selectedClassId;
    private int _selectedSubId;
    private int _selectedFieldId;

    public ProductClassificationView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += async (_, _) => await LoadClassificationsAsync();
    }

    // === Classifications ===

    private async Task LoadClassificationsAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar classificacoes", async () =>
        {
            ClassGrid.ItemsSource = await _service.GetClassificationsAsync();
        });
    }

    private void ClassGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClassGrid.SelectedItem is not ProductClassification c)
        {
            _selectedClassId = 0;
            UpdateClassButton.IsEnabled = false;
            DeleteClassButton.IsEnabled = false;
            AddSubButton.IsEnabled = false;
            SubClassGrid.ItemsSource = null;
            FieldsGrid.ItemsSource = null;
            return;
        }

        _selectedClassId = c.Id;
        ClassCodeTextBox.Text = c.Code;
        ClassNameTextBox.Text = c.Name;
        UpdateClassButton.IsEnabled = true;
        DeleteClassButton.IsEnabled = true;
        AddSubButton.IsEnabled = true;
        SubClassTitle.Text = $"Sub - {c.Name}";
        _ = LoadSubClassificationsAsync(c.Id);
    }

    private async void AddClassification_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ClassCodeTextBox.Text) || string.IsNullOrWhiteSpace(ClassNameTextBox.Text))
        { MessageBox.Show("Preencha Codigo e Nome.", "Classificacao", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        await ViewHelper.RunSafeAsync("Adicionar classificacao", async () =>
        {
            await _service.AddClassificationAsync(new ProductClassification { Code = ClassCodeTextBox.Text.Trim(), Name = ClassNameTextBox.Text.Trim() });
            ClassCodeTextBox.Clear(); ClassNameTextBox.Clear();
            await LoadClassificationsAsync();
        });
    }

    private async void UpdateClassification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedClassId <= 0) return;
        await ViewHelper.RunSafeAsync("Atualizar classificacao", async () =>
        {
            await _service.UpdateClassificationAsync(new ProductClassification { Id = _selectedClassId, Code = ClassCodeTextBox.Text.Trim(), Name = ClassNameTextBox.Text.Trim() });
            await LoadClassificationsAsync();
        });
    }

    private async void DeleteClassification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedClassId <= 0 || !ViewHelper.Confirm("Excluir esta classificacao e todas as sub-classificacoes?")) return;
        await ViewHelper.RunSafeAsync("Excluir classificacao", async () =>
        {
            await _service.DeleteClassificationAsync(_selectedClassId);
            _selectedClassId = 0;
            await LoadClassificationsAsync();
        });
    }

    // === Sub-classifications ===

    private async Task LoadSubClassificationsAsync(int classificationId)
    {
        await ViewHelper.RunSafeAsync("Carregar sub-classificacoes", async () =>
        {
            SubClassGrid.ItemsSource = await _service.GetSubClassificationsAsync(classificationId);
            FieldsGrid.ItemsSource = null;
            _selectedSubId = 0;
            _selectedFieldId = 0;
        });
    }

    private void SubClassGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubClassGrid.SelectedItem is not ProductSubClassification s)
        {
            _selectedSubId = 0;
            UpdateSubButton.IsEnabled = false;
            DeleteSubButton.IsEnabled = false;
            AddFieldButton.IsEnabled = false;
            FieldsGrid.ItemsSource = null;
            return;
        }

        _selectedSubId = s.Id;
        SubCodeTextBox.Text = s.Code;
        SubNameTextBox.Text = s.Name;
        UpdateSubButton.IsEnabled = true;
        DeleteSubButton.IsEnabled = true;
        AddFieldButton.IsEnabled = true;
        FieldsTitle.Text = $"Campos - {s.Name}";
        _ = LoadFieldsAsync(s.Id);
    }

    private async void AddSubClassification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedClassId <= 0) return;
        if (string.IsNullOrWhiteSpace(SubCodeTextBox.Text) || string.IsNullOrWhiteSpace(SubNameTextBox.Text))
        { MessageBox.Show("Preencha Codigo e Nome.", "Sub-classificacao", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        await ViewHelper.RunSafeAsync("Adicionar sub-classificacao", async () =>
        {
            await _service.AddSubClassificationAsync(new ProductSubClassification { ClassificationId = _selectedClassId, Code = SubCodeTextBox.Text.Trim(), Name = SubNameTextBox.Text.Trim() });
            SubCodeTextBox.Clear(); SubNameTextBox.Clear();
            await LoadSubClassificationsAsync(_selectedClassId);
        });
    }

    private async void UpdateSubClassification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSubId <= 0) return;
        await ViewHelper.RunSafeAsync("Atualizar sub-classificacao", async () =>
        {
            await _service.UpdateSubClassificationAsync(new ProductSubClassification { Id = _selectedSubId, ClassificationId = _selectedClassId, Code = SubCodeTextBox.Text.Trim(), Name = SubNameTextBox.Text.Trim() });
            await LoadSubClassificationsAsync(_selectedClassId);
        });
    }

    private async void DeleteSubClassification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSubId <= 0 || !ViewHelper.Confirm("Excluir esta sub-classificacao e seus campos?")) return;
        await ViewHelper.RunSafeAsync("Excluir sub-classificacao", async () =>
        {
            await _service.DeleteSubClassificationAsync(_selectedSubId);
            _selectedSubId = 0;
            await LoadSubClassificationsAsync(_selectedClassId);
        });
    }

    // === Custom Fields ===

    private async Task LoadFieldsAsync(int subClassificationId)
    {
        await ViewHelper.RunSafeAsync("Carregar campos", async () =>
        {
            FieldsGrid.ItemsSource = await _service.GetCustomFieldsAsync(subClassificationId);
            _selectedFieldId = 0;
        });
    }

    private void FieldsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FieldsGrid.SelectedItem is not ProductCustomField f)
        {
            _selectedFieldId = 0;
            UpdateFieldButton.IsEnabled = false;
            DeleteFieldButton.IsEnabled = false;
            return;
        }

        _selectedFieldId = f.Id;
        FieldNameTextBox.Text = f.FieldName;
        FieldRequiredCheckBox.IsChecked = f.IsRequired;
        UpdateFieldButton.IsEnabled = true;
        DeleteFieldButton.IsEnabled = true;
    }

    private async void AddField_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSubId <= 0) return;
        if (string.IsNullOrWhiteSpace(FieldNameTextBox.Text))
        { MessageBox.Show("Preencha o nome do campo.", "Campo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        await ViewHelper.RunSafeAsync("Adicionar campo", async () =>
        {
            await _service.AddCustomFieldAsync(new ProductCustomField { SubClassificationId = _selectedSubId, FieldName = FieldNameTextBox.Text.Trim(), IsRequired = FieldRequiredCheckBox.IsChecked == true });
            FieldNameTextBox.Clear(); FieldRequiredCheckBox.IsChecked = false;
            await LoadFieldsAsync(_selectedSubId);
        });
    }

    private async void UpdateField_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFieldId <= 0) return;
        await ViewHelper.RunSafeAsync("Atualizar campo", async () =>
        {
            await _service.UpdateCustomFieldAsync(new ProductCustomField { Id = _selectedFieldId, SubClassificationId = _selectedSubId, FieldName = FieldNameTextBox.Text.Trim(), IsRequired = FieldRequiredCheckBox.IsChecked == true });
            await LoadFieldsAsync(_selectedSubId);
        });
    }

    private async void DeleteField_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFieldId <= 0 || !ViewHelper.Confirm("Excluir este campo?")) return;
        await ViewHelper.RunSafeAsync("Excluir campo", async () =>
        {
            await _service.DeleteCustomFieldAsync(_selectedFieldId);
            _selectedFieldId = 0;
            await LoadFieldsAsync(_selectedSubId);
        });
    }
}
