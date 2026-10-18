using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Erp.Application;
using Erp.Modules.Sales;
using Microsoft.Win32;

namespace Erp.Desktop.Wpf.Views;

public partial class ProductCatalogView : UserControl
{
    private readonly SalesApplicationService _service;
    private int _selectedId;
    private bool _isBusy;
    private bool _isLoadingCustomFields;
    private int _customFieldsLoadVersion;
    private string? _storedImagePath;
    private string? _pendingImageSourcePath;
    private IReadOnlyList<Product> _products = [];
    private IReadOnlyList<ProductClassification> _classifications = [];
    private IReadOnlyList<ProductSubClassification> _allSubClassifications = [];
    private IReadOnlyList<ProductBrand> _brands = [];
    private IReadOnlyList<ProductCustomField> _currentCustomFields = [];
    private readonly List<(int FieldId, TextBox TextBox)> _customFieldControls = [];

    public ProductCatalogView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
        UnitPriceTextBox.TextChanged += PriceChanged;
        CostPriceTextBox.TextChanged += PriceChanged;
        SubClassComboBox.SelectionChanged += SubClassComboBox_SelectionChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadClassificationsAsync();
        await LoadBrandsAsync();
        await LoadGridAsync();
    }

    private async Task LoadClassificationsAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar classificacoes", async () =>
        {
            _classifications = await _service.GetClassificationsAsync();
            _allSubClassifications = await _service.GetAllSubClassificationsAsync();

            var classItems = _classifications.Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" }).ToList();
            classItems.Insert(0, new { Id = 0, Display = "(nenhuma)" });
            ClassComboBox.ItemsSource = classItems;
            ClassComboBox.SelectedIndex = 0;
        });
    }

    private async Task LoadBrandsAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar marcas", async () =>
        {
            _brands = await _service.GetBrandsAsync();
            var brandItems = _brands.Select(b => new { b.Id, Display = $"{b.Code} - {b.Name}" }).ToList();
            brandItems.Insert(0, new { Id = 0, Display = "(nenhuma)" });
            BrandComboBox.ItemsSource = brandItems;
            BrandComboBox.SelectedIndex = 0;
        });
    }

    private async Task LoadGridAsync()
    {
        await ViewHelper.RunSafeBusyAsync("Carregar produtos", SetBusy, async () =>
        {
            _products = await _service.GetProductCatalogItemsAsync();
            ApplyFilter();
        });
    }

    private Task<string> GenerateNextCodeAsync() => _service.GetNextProductCodeAsync();

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product)
        {
            _selectedId = 0;
            EditButton.IsEnabled = false;
            return;
        }

        _selectedId = product.Id;
        EditButton.IsEnabled = !_isBusy && !_isLoadingCustomFields;
    }

    private async Task LoadSelectedProductAsync()
    {
        if (_selectedId <= 0)
            return;

        var detail = await _service.GetProductByIdAsync(_selectedId);
        if (detail is null)
            throw new InvalidOperationException("Produto nao encontrado.");

        _storedImagePath = NormalizeStoredImagePath(detail.ImagePath);
        _pendingImageSourcePath = null;
        CodeTextBox.Text = detail.Code ?? string.Empty;
        SkuTextBox.Text = detail.Sku;
        NameTextBox.Text = detail.Name;
        DescriptionTextBox.Text = detail.Description;
        BarcodeTextBox.Text = detail.Barcode;
        IsActiveCheckBox.IsChecked = detail.IsActive;
        UnitPriceTextBox.Text = detail.UnitPrice.ToString("N2", CultureInfo.CurrentCulture);
        CostPriceTextBox.Text = detail.CostPrice.ToString("N2", CultureInfo.CurrentCulture);
        UpdateMarginDisplay();
        NcmTextBox.Text = detail.Ncm;
        ColorTextBox.Text = detail.Color;
        WeightTextBox.Text = detail.Weight.ToString("N3", CultureInfo.CurrentCulture);
        WidthTextBox.Text = detail.Width.ToString("N2", CultureInfo.CurrentCulture);
        HeightTextBox.Text = detail.Height.ToString("N2", CultureInfo.CurrentCulture);
        DepthTextBox.Text = detail.Depth.ToString("N2", CultureInfo.CurrentCulture);
        ImagePathTextBox.Text = Path.GetFileName(_storedImagePath ?? string.Empty);
        LoadImagePreview(ResolveImagePath(_storedImagePath));
        SelectComboByTag(VoltageComboBox, detail.Voltage ?? string.Empty);

        BrandComboBox.SelectedValue = detail.BrandId ?? 0;

        if (detail.SubClassificationId.HasValue)
        {
            var sub = _allSubClassifications.FirstOrDefault(s => s.Id == detail.SubClassificationId.Value);
            if (sub != null)
            {
                ClassComboBox.SelectedValue = sub.ClassificationId;
                await Dispatcher.InvokeAsync(() => SubClassComboBox.SelectedValue = detail.SubClassificationId.Value);
                await LoadCustomFieldsForSubAsync(detail.SubClassificationId.Value);
                return;
            }
        }

        ClassComboBox.SelectedIndex = 0;
        HideCustomFields();
    }

    private void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClassComboBox.SelectedValue is int classId && classId > 0)
        {
            var subItems = _allSubClassifications
                .Where(s => s.ClassificationId == classId)
                .Select(s => new { s.Id, Display = $"{s.Code} - {s.Name}" })
                .ToList();
            subItems.Insert(0, new { Id = 0, Display = "(nenhuma)" });
            SubClassComboBox.ItemsSource = subItems;
            SubClassComboBox.SelectedIndex = 0;
            return;
        }

        SubClassComboBox.ItemsSource = null;
        HideCustomFields();
    }

    private async void SubClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubClassComboBox.SelectedValue is int subId && subId > 0)
        {
            await LoadCustomFieldsForSubAsync(subId);
            return;
        }

        HideCustomFields();
    }

    private async Task LoadCustomFieldsForSubAsync(int subId)
    {
        var loadVersion = ++_customFieldsLoadVersion;
        SetCustomFieldsLoading(true);

        await ViewHelper.RunSafeAsync("Carregar campos", async () =>
        {
            try
            {
                _currentCustomFields = await _service.GetCustomFieldsAsync(subId);
                if (loadVersion != _customFieldsLoadVersion)
                    return;

                _customFieldControls.Clear();
                CustomFieldsHost.Items.Clear();

                if (_currentCustomFields.Count == 0)
                {
                    CustomFieldsPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                var sub = _allSubClassifications.FirstOrDefault(s => s.Id == subId);
                CustomFieldsTitle.Text = $"Campos - {sub?.Name ?? string.Empty}";

                IReadOnlyList<(int FieldId, string FieldName, string Value)> existingValues = [];
                if (_selectedId > 0)
                    existingValues = await _service.GetProductCustomValuesAsync(_selectedId);

                if (loadVersion != _customFieldsLoadVersion)
                    return;

                foreach (var field in _currentCustomFields)
                {
                    var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };
                    var label = field.IsRequired ? $"{field.FieldName} *" : field.FieldName;
                    panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, FontSize = 12 });
                    var textBox = new TextBox();
                    var existing = existingValues.FirstOrDefault(v => v.FieldId == field.Id);
                    if (existing.FieldId > 0)
                        textBox.Text = existing.Value;
                    panel.Children.Add(textBox);
                    CustomFieldsHost.Items.Add(panel);
                    _customFieldControls.Add((field.Id, textBox));
                }

                CustomFieldsPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                if (loadVersion == _customFieldsLoadVersion)
                    SetCustomFieldsLoading(false);
            }
        });
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeBusyAsync("Novo produto", SetBusy, async () =>
        {
            _selectedId = 0;
            ClearFields();
            CodeTextBox.Text = await GenerateNextCodeAsync();
            ProductsGrid.SelectedItem = null;
            EditButton.IsEnabled = false;
            FormTitle.Text = "Novo Produto";
            FormPanel.Visibility = Visibility.Visible;
            await Dispatcher.InvokeAsync(() => SkuTextBox.Focus());
        });
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0)
            return;

        await ViewHelper.RunSafeBusyAsync("Carregar produto", SetBusy, async () =>
        {
            await LoadSelectedProductAsync();
            FormTitle.Text = "Editar Produto";
            FormPanel.Visibility = Visibility.Visible;
            await Dispatcher.InvokeAsync(() => SkuTextBox.Focus());
        });
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        _selectedId = 0;
        ClearFields();
        ProductsGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
    }

    private void PriceChanged(object? sender, TextChangedEventArgs e) => UpdateMarginDisplay();

    private void UpdateMarginDisplay()
    {
        ViewHelper.TryDecimal(UnitPriceTextBox.Text, out var salePrice);
        ViewHelper.TryDecimal(CostPriceTextBox.Text, out var costPrice);
        if (costPrice > 0)
            MarginTextBox.Text = ((salePrice - costPrice) / costPrice * 100).ToString("N2", CultureInfo.CurrentCulture) + "%";
        else
            MarginTextBox.Text = salePrice > 0 ? "---" : "0,00%";
    }

    private void SelectImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Todos|*.*",
            Title = "Selecionar imagem do produto"
        };

        if (dialog.ShowDialog() != true)
            return;

        _pendingImageSourcePath = dialog.FileName;
        ImagePathTextBox.Text = Path.GetFileName(dialog.FileName);
        LoadImagePreview(dialog.FileName);
    }

    private void LoadImagePreview(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            ProductImage.Source = null;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            ProductImage.Source = bitmap;
        }
        catch
        {
            ProductImage.Source = null;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeBusyAsync("Salvar produto", SetBusy, async () =>
        {
            if (_isLoadingCustomFields)
                throw new InvalidOperationException("Aguarde o carregamento dos campos da classificacao antes de salvar.");

            if (!Validate(out var unitPrice, out var costPrice, out var weight, out var width, out var height, out var depth))
                return;

            var subClassId = SubClassComboBox.SelectedValue is int subId && subId > 0 ? (int?)subId : null;
            var brandId = BrandComboBox.SelectedValue is int bId && bId > 0 ? (int?)bId : null;
            var voltageItem = VoltageComboBox.SelectedItem as ComboBoxItem;
            var voltage = voltageItem?.Tag?.ToString();
            var preparedImage = await PrepareImageAsync();

            try
            {
                var product = new Product
                {
                    Id = _selectedId,
                    Code = CodeTextBox.Text.Trim(),
                    Sku = SkuTextBox.Text.Trim(),
                    Name = NameTextBox.Text.Trim(),
                    Description = Opt(DescriptionTextBox.Text),
                    Barcode = NormalizeDigitsOrNull(BarcodeTextBox.Text),
                    IsActive = IsActiveCheckBox.IsChecked != false,
                    UnitPrice = unitPrice,
                    CostPrice = costPrice,
                    Quantity = 0,
                    ImagePath = preparedImage.StoredPath,
                    Color = Opt(ColorTextBox.Text),
                    SubClassificationId = subClassId,
                    BrandId = brandId,
                    Ncm = NormalizeDigitsOrNull(NcmTextBox.Text),
                    Weight = weight,
                    Width = width,
                    Height = height,
                    Depth = depth,
                    Voltage = string.IsNullOrWhiteSpace(voltage) ? null : voltage
                };

                var values = _customFieldControls
                    .Where(c => !string.IsNullOrWhiteSpace(c.TextBox.Text))
                    .Select(c => (c.FieldId, c.TextBox.Text.Trim()))
                    .ToList();

                await _service.SaveProductAsync(product, values);
                _storedImagePath = preparedImage.StoredPath;
                _pendingImageSourcePath = null;

                FormPanel.Visibility = Visibility.Collapsed;
                ClearFields();
                _selectedId = 0;
                await LoadGridAsync();
            }
            catch
            {
                DeleteFileIfExists(preparedImage.AbsolutePath);
                throw;
            }
        });
    }

    private bool Validate(out decimal unitPrice, out decimal costPrice, out decimal weight, out decimal width, out decimal height, out decimal depth)
    {
        unitPrice = 0;
        costPrice = 0;
        weight = 0;
        width = 0;
        height = 0;
        depth = 0;
        var errors = new List<string>();
        Control? firstInvalid = null;

        void AddError(Control control, string message)
        {
            firstInvalid ??= control;
            errors.Add(message);
        }

        if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            AddError(CodeTextBox, "Codigo interno e obrigatorio.");
        if (string.IsNullOrWhiteSpace(SkuTextBox.Text))
            AddError(SkuTextBox, "SKU e obrigatorio.");
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            AddError(NameTextBox, "Nome e obrigatorio.");
        if (!ViewHelper.TryDecimal(UnitPriceTextBox.Text, out unitPrice) || unitPrice < 0)
            AddError(UnitPriceTextBox, "Preco de venda invalido.");
        if (!ViewHelper.TryDecimal(CostPriceTextBox.Text, out costPrice) || costPrice < 0)
            AddError(CostPriceTextBox, "Preco de custo invalido.");
        if (!TryReadNonNegativeDecimal(WeightTextBox, "Peso", out weight, out var weightError))
            AddError(WeightTextBox, weightError);
        if (!TryReadNonNegativeDecimal(WidthTextBox, "Largura", out width, out var widthError))
            AddError(WidthTextBox, widthError);
        if (!TryReadNonNegativeDecimal(HeightTextBox, "Altura", out height, out var heightError))
            AddError(HeightTextBox, heightError);
        if (!TryReadNonNegativeDecimal(DepthTextBox, "Profundidade", out depth, out var depthError))
            AddError(DepthTextBox, depthError);

        var barcode = CleanDigits(BarcodeTextBox.Text);
        if (!string.IsNullOrEmpty(barcode) && barcode.Length is not (8 or 12 or 13 or 14))
            AddError(BarcodeTextBox, "Codigo de barras deve ter 8, 12, 13 ou 14 digitos.");

        var ncm = CleanDigits(NcmTextBox.Text);
        if (!string.IsNullOrEmpty(ncm) && ncm.Length != 8)
            AddError(NcmTextBox, "NCM deve ter 8 digitos.");

        foreach (var field in _currentCustomFields.Where(f => f.IsRequired))
        {
            var control = _customFieldControls.FirstOrDefault(c => c.FieldId == field.Id);
            if (control.TextBox != null && string.IsNullOrWhiteSpace(control.TextBox.Text))
                AddError(control.TextBox, $"Campo '{field.FieldName}' e obrigatorio.");
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
            _ = Dispatcher.InvokeAsync(() => firstInvalid?.Focus());
            return false;
        }

        return true;
    }

    private static bool TryReadNonNegativeDecimal(TextBox textBox, string label, out decimal value, out string error)
    {
        value = 0;
        error = $"{label} invalido.";
        if (!ViewHelper.TryDecimal(textBox.Text, out value))
            return false;

        if (value < 0)
        {
            error = $"{label} nao pode ser negativo.";
            return false;
        }

        return true;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var term = SearchTextBox.Text.Trim();
        IEnumerable<Product> filtered = _products;

        if (!string.IsNullOrWhiteSpace(term))
        {
            filtered = filtered.Where(p =>
                Contains(p.Code, term) ||
                Contains(p.Sku, term) ||
                Contains(p.Barcode, term) ||
                Contains(p.Name, term) ||
                Contains(p.Color, term) ||
                Contains(p.ClassificationDisplay, term) ||
                Contains(p.SubClassificationDisplay, term) ||
                Contains(p.BrandDisplay, term));
        }

        ProductsGrid.ItemsSource = filtered.ToList();
    }

    private static bool Contains(string? source, string term)
        => !string.IsNullOrWhiteSpace(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

    private void ClearFields()
    {
        CodeTextBox.Clear();
        SkuTextBox.Clear();
        NameTextBox.Clear();
        DescriptionTextBox.Clear();
        BarcodeTextBox.Clear();
        UnitPriceTextBox.Text = "0,00";
        CostPriceTextBox.Text = "0,00";
        MarginTextBox.Clear();
        NcmTextBox.Clear();
        ColorTextBox.Clear();
        WeightTextBox.Text = "0";
        WidthTextBox.Text = "0";
        HeightTextBox.Text = "0";
        DepthTextBox.Text = "0";
        ImagePathTextBox.Clear();
        ProductImage.Source = null;
        IsActiveCheckBox.IsChecked = true;
        ClassComboBox.SelectedIndex = 0;
        VoltageComboBox.SelectedIndex = 0;
        BrandComboBox.SelectedIndex = 0;
        HideCustomFields();
        _storedImagePath = null;
        _pendingImageSourcePath = null;
    }

    private void HideCustomFields()
    {
        CustomFieldsPanel.Visibility = Visibility.Collapsed;
        _currentCustomFields = [];
        _customFieldControls.Clear();
        CustomFieldsHost.Items.Clear();
    }

    private static string? Opt(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string CleanDigits(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

    private static string? NormalizeDigitsOrNull(string? value)
    {
        var digits = CleanDigits(value);
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static void SelectComboByTag(ComboBox combo, string tag)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        combo.SelectedIndex = 0;
    }

    private async Task<(string? StoredPath, string? AbsolutePath)> PrepareImageAsync()
    {
        if (string.IsNullOrWhiteSpace(_pendingImageSourcePath))
            return (NormalizeStoredImagePath(_storedImagePath), null);

        if (!File.Exists(_pendingImageSourcePath))
            throw new InvalidOperationException("Arquivo de imagem nao encontrado.");

        var appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErpSystem");
        var imageDirectory = Path.Combine(appDataRoot, "product-images");
        Directory.CreateDirectory(imageDirectory);

        var extension = Path.GetExtension(_pendingImageSourcePath);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(imageDirectory, fileName);

        await using var source = File.OpenRead(_pendingImageSourcePath);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);

        var relativePath = Path.Combine("product-images", fileName).Replace('\\', '/');
        return (relativePath, destinationPath);
    }

    private static string? NormalizeStoredImagePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return null;

        return Path.IsPathRooted(storedPath) ? null : storedPath.Replace('\\', '/');
    }

    private static string? ResolveImagePath(string? storedPath)
    {
        var normalized = NormalizeStoredImagePath(storedPath);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ErpSystem");
        return Path.Combine(appDataRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void DeleteFileIfExists(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            File.Delete(path);
    }

    private void SetCustomFieldsLoading(bool isLoading)
    {
        _isLoadingCustomFields = isLoading;
        ApplyBusyState();
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        ApplyBusyState();
    }

    private void ApplyBusyState()
    {
        var isLocked = _isBusy || _isLoadingCustomFields;
        NewButton.IsEnabled = !isLocked;
        EditButton.IsEnabled = !isLocked && _selectedId > 0;
        SaveButton.IsEnabled = !isLocked;
        CancelFormButton.IsEnabled = !isLocked;
        SelectImageButton.IsEnabled = !isLocked;
        SearchTextBox.IsEnabled = !isLocked;
        ProductsGrid.IsEnabled = !isLocked;
        FormPanel.IsEnabled = !isLocked;
        ClassComboBox.IsEnabled = !isLocked;
        SubClassComboBox.IsEnabled = !isLocked;
        VoltageComboBox.IsEnabled = !isLocked;
        BrandComboBox.IsEnabled = !isLocked;
    }
}
