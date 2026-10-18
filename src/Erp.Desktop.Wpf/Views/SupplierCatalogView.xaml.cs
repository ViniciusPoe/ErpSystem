using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Inventory;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class SupplierCatalogView : UserControl
{
    private readonly InventoryApplicationService _inventoryService;
    private readonly SalesApplicationService _salesService;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private int _selectedId;
    private bool _isBusy;
    private IReadOnlyList<Supplier> _suppliers = [];
    private IReadOnlyList<Municipality> _municipalities = [];

    public SupplierCatalogView(InventoryApplicationService inventoryService, SalesApplicationService salesService)
    {
        InitializeComponent();
        _inventoryService = inventoryService;
        _salesService = salesService;
        Loaded += OnLoaded;

        TaxIdTextBox.TextChanged += OnDigitsOnlyTextChanged;
        PhoneTextBox.TextChanged += OnDigitsOnlyTextChanged;
        ZipCodeTextBox.TextChanged += OnDigitsOnlyTextChanged;

        SegmentComboBox.ItemsSource = new[]
        {
            "Alimentos e Bebidas", "Automotivo", "Construcao Civil", "Educacao",
            "Eletronicos e Informatica", "Embalagens", "Energia", "Farmaceutico",
            "Logistica e Transporte", "Maquinas e Equipamentos", "Metalurgica",
            "Moveis e Decoracao", "Papel e Celulose", "Quimico e Petroquimico",
            "Saude", "Servicos", "Tecnologia", "Telecomunicacoes",
            "Textil e Confeccoes", "Varejo", "Agronegocio", "Mineracao"
        };
    }

    private bool _suppressTextChanged;

    private int MaxDigitsFor(TextBox tb)
    {
        if (tb == TaxIdTextBox) return 14;
        if (tb == PhoneTextBox) return 11;
        if (tb == ZipCodeTextBox) return 8;
        return int.MaxValue;
    }

    private void OnDigitsOnlyTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || sender is not TextBox tb)
            return;

        var cleaned = CleanDigits(tb.Text);
        var max = MaxDigitsFor(tb);
        if (cleaned.Length > max)
            cleaned = cleaned[..max];

        if (cleaned == tb.Text)
            return;

        _suppressTextChanged = true;
        var caret = tb.CaretIndex;
        var diff = tb.Text.Length - cleaned.Length;
        tb.Text = cleaned;
        tb.CaretIndex = Math.Max(0, Math.Min(caret - diff, cleaned.Length));
        _suppressTextChanged = false;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadMunicipalitiesAsync();
        await ReloadAsync();
    }

    private async Task LoadMunicipalitiesAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar municipios", async () =>
        {
            _municipalities = await _salesService.GetMunicipalitiesAsync();
            var items = _municipalities.Select(m => new { m.Id, Display = $"{m.Name} - {m.State}" }).ToList();
            items.Insert(0, new { Id = 0, Display = "(selecione)" });
            MunicipalityComboBox.ItemsSource = items;
            MunicipalityComboBox.SelectedIndex = 0;
        });
    }

    private async Task ReloadAsync()
    {
        await ViewHelper.RunSafeBusyAsync("Carregar fornecedores", SetBusy, async () =>
        {
            _suppliers = await _inventoryService.GetSupplierCatalogItemsAsync();
            ApplyFilter();
        });
    }

    private Task<string> GenerateNextCodeAsync() => _inventoryService.GetNextSupplierCodeAsync();

    private void SuppliersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuppliersGrid.SelectedItem is not Supplier supplier)
        {
            _selectedId = 0;
            EditButton.IsEnabled = false;
            return;
        }

        _selectedId = supplier.Id;
        EditButton.IsEnabled = !_isBusy;
    }

    private async Task LoadSelectedSupplierAsync()
    {
        if (_selectedId <= 0)
            return;

        var detail = await _inventoryService.GetSupplierByIdAsync(_selectedId);
        if (detail is null)
            throw new InvalidOperationException("Fornecedor nao encontrado.");

        CodeTextBox.Text = detail.Code;
        NameTextBox.Text = detail.Name;
        TaxIdTextBox.Text = detail.TaxId;
        IsActiveCheckBox.IsChecked = detail.IsActive;
        ContactTextBox.Text = detail.ContactName;
        EmailTextBox.Text = detail.Email;
        PhoneTextBox.Text = detail.Phone;
        ZipCodeTextBox.Text = detail.ZipCode;
        StreetTextBox.Text = detail.Street;
        NumberTextBox.Text = detail.Number;
        DistrictTextBox.Text = detail.District;
        CityTextBox.Text = detail.City;
        StateTextBox.Text = detail.State;
        MinimumOrderTextBox.Text = detail.MinimumOrder > 0 ? detail.MinimumOrder.ToString("N2") : string.Empty;
        SegmentComboBox.Text = detail.Segment;

        var match = _municipalities.FirstOrDefault(m =>
            string.Equals(m.Name, detail.City, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(m.State, detail.State, StringComparison.OrdinalIgnoreCase));
        MunicipalityComboBox.SelectedValue = match?.Id ?? 0;
    }

    private void MunicipalityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MunicipalityComboBox.SelectedValue is int id && id > 0)
        {
            var municipality = _municipalities.FirstOrDefault(m => m.Id == id);
            if (municipality != null)
            {
                CityTextBox.Text = municipality.Name;
                StateTextBox.Text = municipality.State;
            }
            return;
        }

        CityTextBox.Clear();
        StateTextBox.Clear();
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeBusyAsync("Novo fornecedor", SetBusy, async () =>
        {
            _selectedId = 0;
            ClearFields();
            CodeTextBox.Text = await GenerateNextCodeAsync();
            CodeTextBox.IsReadOnly = true;
            SuppliersGrid.SelectedItem = null;
            EditButton.IsEnabled = false;
            FormTitle.Text = "Novo Fornecedor";
            FormPanel.Visibility = Visibility.Visible;
            await Dispatcher.InvokeAsync(() => NameTextBox.Focus());
        });
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0)
            return;

        await ViewHelper.RunSafeBusyAsync("Carregar fornecedor", SetBusy, async () =>
        {
            await LoadSelectedSupplierAsync();
            CodeTextBox.IsReadOnly = true;
            FormTitle.Text = "Editar Fornecedor";
            FormPanel.Visibility = Visibility.Visible;
            await Dispatcher.InvokeAsync(() => NameTextBox.Focus());
        });
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        _selectedId = 0;
        ClearFields();
        SuppliersGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeBusyAsync("Salvar fornecedor", SetBusy, async () =>
        {
            if (!ValidateForm())
                return;

            var supplier = new Supplier
            {
                Id = _selectedId,
                Code = CodeTextBox.Text.Trim().ToUpperInvariant(),
                Name = NameTextBox.Text.Trim(),
                TaxId = CleanDigits(TaxIdTextBox.Text),
                IsActive = IsActiveCheckBox.IsChecked != false,
                ContactName = Opt(ContactTextBox.Text),
                Email = Opt(EmailTextBox.Text),
                Phone = Opt(CleanDigits(PhoneTextBox.Text)),
                Street = Opt(StreetTextBox.Text),
                Number = Opt(NumberTextBox.Text),
                District = Opt(DistrictTextBox.Text),
                City = Opt(CityTextBox.Text),
                State = Opt(StateTextBox.Text)?.ToUpperInvariant(),
                ZipCode = Opt(CleanDigits(ZipCodeTextBox.Text)),
                MinimumOrder = ViewHelper.ReadDecimal(MinimumOrderTextBox.Text),
                Segment = Opt(SegmentComboBox.Text)
            };

            if (_selectedId > 0)
            {
                await _inventoryService.UpdateSupplierAsync(supplier);
            }
            else
            {
                await _inventoryService.AddSupplierAsync(
                    supplier.Code,
                    supplier.Name,
                    supplier.TaxId,
                    supplier.IsActive,
                    supplier.ContactName,
                    supplier.Email,
                    supplier.Phone,
                    supplier.Street,
                    supplier.Number,
                    supplier.District,
                    supplier.City,
                    supplier.State,
                    supplier.ZipCode,
                    supplier.MinimumOrder,
                    supplier.Segment);
            }

            FormPanel.Visibility = Visibility.Collapsed;
            ClearFields();
            _selectedId = 0;
            await ReloadAsync();
        });
    }

    private async void SearchCepButton_Click(object sender, RoutedEventArgs e)
    {
        await SearchCepAsync();
    }

    private async Task SearchCepAsync()
    {
        var cep = CleanDigits(ZipCodeTextBox.Text);
        if (cep.Length != 8)
        {
            MessageBox.Show("Informe um CEP com 8 digitos para consultar.", "CEP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ViewHelper.RunSafeBusyAsync("Buscar CEP", SetBusy, async () =>
        {
            var json = await HttpClient.GetStringAsync($"https://viacep.com.br/ws/{cep}/json/");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("erro", out _))
                throw new InvalidOperationException("CEP nao encontrado.");

            if (root.TryGetProperty("logradouro", out var logradouro) && logradouro.GetString() is string street && street.Length > 0)
                StreetTextBox.Text = street;

            if (root.TryGetProperty("bairro", out var bairro) && bairro.GetString() is string district && district.Length > 0)
                DistrictTextBox.Text = district;

            if (root.TryGetProperty("localidade", out var localidade) && localidade.GetString() is string city)
            {
                CityTextBox.Text = city;

                if (root.TryGetProperty("uf", out var ufProp) && ufProp.GetString() is string state)
                {
                    StateTextBox.Text = state.ToUpperInvariant();
                    var match = _municipalities.FirstOrDefault(m =>
                        string.Equals(m.Name, city, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.State, state, StringComparison.OrdinalIgnoreCase));
                    MunicipalityComboBox.SelectedValue = match?.Id ?? 0;
                }
            }

            ZipCodeTextBox.Text = cep;
        });
    }

    private bool ValidateForm()
    {
        var errors = new List<string>();
        Control? firstInvalid = null;

        void AddError(Control control, string message)
        {
            firstInvalid ??= control;
            errors.Add(message);
        }

        var code = CodeTextBox.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            AddError(CodeTextBox, "Codigo e obrigatorio.");
        else if (!code.StartsWith('F'))
            AddError(CodeTextBox, "Codigo deve comecar com 'F' (ex: F001).");

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            AddError(NameTextBox, "Nome/Razao Social e obrigatorio.");

        var cnpjDigits = CleanDigits(TaxIdTextBox.Text);
        if (string.IsNullOrEmpty(cnpjDigits))
            AddError(TaxIdTextBox, "CNPJ e obrigatorio.");
        else if (cnpjDigits.Length != 14)
            AddError(TaxIdTextBox, "CNPJ deve ter 14 digitos.");
        else if (!ValidateCnpj(cnpjDigits))
            AddError(TaxIdTextBox, "CNPJ invalido.");

        var email = Opt(EmailTextBox.Text);
        if (email != null && !IsValidEmail(email))
            AddError(EmailTextBox, "Email invalido. Deve conter '@' e um dominio valido.");

        var phoneDigits = CleanDigits(PhoneTextBox.Text);
        if (!string.IsNullOrEmpty(phoneDigits) && phoneDigits.Length is not (10 or 11))
            AddError(PhoneTextBox, "Telefone deve ter 10 digitos (fixo) ou 11 digitos (celular) incluindo DDD.");

        var cepDigits = CleanDigits(ZipCodeTextBox.Text);
        if (!string.IsNullOrEmpty(cepDigits) && cepDigits.Length != 8)
            AddError(ZipCodeTextBox, "CEP deve ter 8 digitos.");

        var minOrderText = MinimumOrderTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(minOrderText) && !ViewHelper.TryDecimal(minOrderText, out _))
            AddError(MinimumOrderTextBox, "Pedido minimo deve ser um valor numerico valido.");

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
            _ = Dispatcher.InvokeAsync(() => firstInvalid?.Focus());
            return false;
        }

        return true;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var term = SearchTextBox.Text.Trim();
        IEnumerable<Supplier> filtered = _suppliers;

        if (!string.IsNullOrWhiteSpace(term))
        {
            filtered = filtered.Where(s =>
                Contains(s.Code, term) ||
                Contains(s.Name, term) ||
                Contains(s.TaxId, term) ||
                Contains(s.City, term) ||
                Contains(s.State, term) ||
                Contains(s.Segment, term));
        }

        SuppliersGrid.ItemsSource = filtered.ToList();
    }

    private static bool Contains(string? source, string term)
        => !string.IsNullOrWhiteSpace(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool ValidateCnpj(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
            return false;

        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (cnpj[i] - '0') * weights1[i];

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if ((cnpj[12] - '0') != digit1)
            return false;

        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += (cnpj[i] - '0') * weights2[i];

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return (cnpj[13] - '0') == digit2;
    }

    private static bool IsValidEmail(string email)
        => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static string CleanDigits(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

    private void ClearFields()
    {
        CodeTextBox.Clear();
        NameTextBox.Clear();
        TaxIdTextBox.Clear();
        ContactTextBox.Clear();
        EmailTextBox.Clear();
        PhoneTextBox.Clear();
        ZipCodeTextBox.Clear();
        CityTextBox.Clear();
        StreetTextBox.Clear();
        NumberTextBox.Clear();
        DistrictTextBox.Clear();
        StateTextBox.Clear();
        MinimumOrderTextBox.Clear();
        SegmentComboBox.Text = string.Empty;
        IsActiveCheckBox.IsChecked = true;
        MunicipalityComboBox.SelectedIndex = 0;
    }

    private static string? Opt(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        NewButton.IsEnabled = !isBusy;
        EditButton.IsEnabled = !isBusy && _selectedId > 0;
        SaveButton.IsEnabled = !isBusy;
        CancelFormButton.IsEnabled = !isBusy;
        SearchCepButton.IsEnabled = !isBusy;
        SearchTextBox.IsEnabled = !isBusy;
        SuppliersGrid.IsEnabled = !isBusy;
        FormPanel.IsEnabled = !isBusy;
        MunicipalityComboBox.IsEnabled = !isBusy;
    }
}
