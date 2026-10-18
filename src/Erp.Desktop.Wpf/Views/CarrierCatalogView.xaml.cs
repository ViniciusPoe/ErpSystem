using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class CarrierCatalogView : UserControl
{
    private readonly SalesApplicationService _service;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private int _selectedId;
    private IReadOnlyList<Carrier> _carriers = [];
    private IReadOnlyList<Municipality> _municipalities = [];
    private readonly List<ServiceAreaItem> _serviceAreas = [];

    public CarrierCatalogView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;

        DocumentTextBox.TextChanged += OnDigitsOnlyTextChanged;
        PhoneTextBox.TextChanged += OnDigitsOnlyTextChanged;
        ZipCodeTextBox.TextChanged += OnDigitsOnlyTextChanged;
    }

    private bool _suppressTextChanged;

    private int MaxDigitsFor(TextBox tb)
    {
        if (tb == DocumentTextBox) return 14;
        if (tb == PhoneTextBox) return 11;
        if (tb == ZipCodeTextBox) return 8;
        return int.MaxValue;
    }

    private void OnDigitsOnlyTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged) return;
        if (sender is not TextBox tb) return;

        var cleaned = CleanDigits(tb.Text);
        var max = MaxDigitsFor(tb);
        if (cleaned.Length > max)
            cleaned = cleaned[..max];

        if (cleaned == tb.Text) return;

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
        await LoadGridAsync();
    }

    private async Task LoadMunicipalitiesAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar municipios", async () =>
        {
            _municipalities = await _service.GetMunicipalitiesAsync();
            var items = _municipalities.Select(m => new { m.Id, Display = $"{m.Name} - {m.State}" }).ToList();
            items.Insert(0, new { Id = 0, Display = "(selecione)" });
            MunicipalityComboBox.ItemsSource = items;
            MunicipalityComboBox.SelectedIndex = 0;
            ServiceAreaComboBox.ItemsSource = items;
            ServiceAreaComboBox.SelectedIndex = 0;
        });
    }

    private async Task LoadGridAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar transportadoras", async () =>
        {
            _carriers = await _service.GetCarriersAsync();
            CarriersGrid.ItemsSource = _carriers;
        });
    }

    private string GenerateNextCode()
    {
        var maxNumber = 0;
        foreach (var c in _carriers)
        {
            if (c.Code.Length > 1 && c.Code.StartsWith('T') && int.TryParse(c.Code[1..], out var num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }
        return $"T{(maxNumber + 1):D3}";
    }

    private void CarriersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CarriersGrid.SelectedItem is not Carrier c)
        {
            EditButton.IsEnabled = false;
            return;
        }

        _selectedId = c.Id;
        CodeTextBox.Text = c.Code;
        NameTextBox.Text = c.Name;
        DocumentTextBox.Text = c.Document;
        ContactTextBox.Text = c.ContactName;
        EmailTextBox.Text = c.Email;
        PhoneTextBox.Text = c.Phone;
        ZipCodeTextBox.Text = c.ZipCode;
        StreetTextBox.Text = c.Street;
        NumberTextBox.Text = c.Number;
        DistrictTextBox.Text = c.District;
        CityTextBox.Text = c.City;
        StateTextBox.Text = c.State;

        var match = _municipalities.FirstOrDefault(m =>
            string.Equals(m.Name, c.City, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(m.State, c.State, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            MunicipalityComboBox.SelectedValue = match.Id;
        else
            MunicipalityComboBox.SelectedIndex = 0;

        _serviceAreas.Clear();
        ServiceAreasListBox.ItemsSource = null;

        EditButton.IsEnabled = true;
    }

    private void MunicipalityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MunicipalityComboBox.SelectedValue is int id && id > 0)
        {
            var mun = _municipalities.FirstOrDefault(m => m.Id == id);
            if (mun != null)
            {
                CityTextBox.Text = mun.Name;
                StateTextBox.Text = mun.State;
            }
        }
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedId = 0;
        ClearFields();
        CodeTextBox.Text = GenerateNextCode();
        CodeTextBox.IsReadOnly = true;
        CarriersGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
        FormTitle.Text = "Nova Transportadora";
        FormPanel.Visibility = Visibility.Visible;
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0) return;
        CodeTextBox.IsReadOnly = true;
        FormTitle.Text = "Editar Transportadora";
        FormPanel.Visibility = Visibility.Visible;

        await LoadServiceAreasAsync(_selectedId);
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        _selectedId = 0;
        ClearFields();
        CarriersGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
    }

    private async Task LoadServiceAreasAsync(int carrierId)
    {
        await ViewHelper.RunSafeAsync("Carregar localidades atendidas", async () =>
        {
            var areas = await _service.GetCarrierServiceAreasAsync(carrierId);
            _serviceAreas.Clear();
            foreach (var m in areas)
                _serviceAreas.Add(new ServiceAreaItem { Id = m.Id, Display = $"{m.Name} - {m.State}" });
            ServiceAreasListBox.ItemsSource = null;
            ServiceAreasListBox.ItemsSource = _serviceAreas;
        });
    }

    private void AddServiceAreaButton_Click(object sender, RoutedEventArgs e)
    {
        if (ServiceAreaComboBox.SelectedValue is not int id || id <= 0) return;

        if (_serviceAreas.Any(a => a.Id == id)) return;

        var mun = _municipalities.FirstOrDefault(m => m.Id == id);
        if (mun == null) return;

        _serviceAreas.Add(new ServiceAreaItem { Id = mun.Id, Display = $"{mun.Name} - {mun.State}" });
        ServiceAreasListBox.ItemsSource = null;
        ServiceAreasListBox.ItemsSource = _serviceAreas;
    }

    private void RemoveServiceAreaButton_Click(object sender, RoutedEventArgs e)
    {
        if (ServiceAreasListBox.SelectedItem is not ServiceAreaItem item) return;

        _serviceAreas.Remove(item);
        ServiceAreasListBox.ItemsSource = null;
        ServiceAreasListBox.ItemsSource = _serviceAreas;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Salvar transportadora", async () =>
        {
            if (!ValidateForm()) return;
            var carrier = new Carrier
            {
                Id = _selectedId,
                Code = CodeTextBox.Text.Trim().ToUpperInvariant(),
                Name = NameTextBox.Text.Trim(),
                Document = CleanDigits(DocumentTextBox.Text),
                ContactName = Opt(ContactTextBox.Text),
                Email = Opt(EmailTextBox.Text),
                Phone = Opt(CleanDigits(PhoneTextBox.Text)),
                Street = Opt(StreetTextBox.Text),
                Number = Opt(NumberTextBox.Text),
                District = Opt(DistrictTextBox.Text),
                City = Opt(CityTextBox.Text),
                State = Opt(StateTextBox.Text)?.ToUpperInvariant(),
                ZipCode = Opt(CleanDigits(ZipCodeTextBox.Text))
            };

            var munIds = _serviceAreas.Select(a => a.Id).ToList();
            await _service.SaveCarrierAsync(carrier, munIds);

            FormPanel.Visibility = Visibility.Collapsed;
            ClearFields();
            _selectedId = 0;
            await LoadGridAsync();
        });
    }

    private async void ZipCodeTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        await SearchCepAsync();
    }

    private async void SearchCepButton_Click(object sender, RoutedEventArgs e)
    {
        await SearchCepAsync();
    }

    private async Task SearchCepAsync()
    {
        var cep = CleanDigits(ZipCodeTextBox.Text);
        if (cep.Length != 8) return;

        await ViewHelper.RunSafeAsync("Buscar CEP", async () =>
        {
            var json = await HttpClient.GetStringAsync($"https://viacep.com.br/ws/{cep}/json/");
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("erro", out _)) return;

            if (root.TryGetProperty("logradouro", out var logradouro) && logradouro.GetString() is string rua && rua.Length > 0)
                StreetTextBox.Text = rua;

            if (root.TryGetProperty("bairro", out var bairro) && bairro.GetString() is string b && b.Length > 0)
                DistrictTextBox.Text = b;

            if (root.TryGetProperty("localidade", out var localidade) && localidade.GetString() is string cidade)
            {
                CityTextBox.Text = cidade;

                if (root.TryGetProperty("uf", out var ufProp) && ufProp.GetString() is string uf)
                {
                    StateTextBox.Text = uf.ToUpperInvariant();

                    var match = _municipalities.FirstOrDefault(m =>
                        string.Equals(m.Name, cidade, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.State, uf, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        MunicipalityComboBox.SelectedValue = match.Id;
                }
            }

            ZipCodeTextBox.Text = cep;
        });
    }

    private bool ValidateForm()
    {
        var errors = new List<string>();

        var code = CodeTextBox.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            errors.Add("Codigo e obrigatorio.");
        else if (!code.StartsWith('T'))
            errors.Add("Codigo deve comecar com 'T' (ex: T001).");

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            errors.Add("Nome/Razao Social e obrigatorio.");

        var cnpjDigits = CleanDigits(DocumentTextBox.Text);
        if (string.IsNullOrEmpty(cnpjDigits))
            errors.Add("CNPJ e obrigatorio.");
        else if (cnpjDigits.Length != 14)
            errors.Add("CNPJ deve ter 14 digitos.");
        else if (!ValidateCnpj(cnpjDigits))
            errors.Add("CNPJ invalido (digitos verificadores nao conferem).");

        var email = Opt(EmailTextBox.Text);
        if (email != null && !IsValidEmail(email))
            errors.Add("Email invalido. Deve conter '@' e um dominio valido (ex: nome@empresa.com).");

        var phoneDigits = CleanDigits(PhoneTextBox.Text);
        if (!string.IsNullOrEmpty(phoneDigits) && phoneDigits.Length is not (10 or 11))
            errors.Add("Telefone deve ter 10 digitos (fixo) ou 11 digitos (celular) incluindo DDD.");

        var cepDigits = CleanDigits(ZipCodeTextBox.Text);
        if (!string.IsNullOrEmpty(cepDigits) && cepDigits.Length != 8)
            errors.Add("CEP deve ter 8 digitos.");

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private static bool ValidateCnpj(string cnpj)
    {
        if (cnpj.Length != 14) return false;
        if (cnpj.Distinct().Count() == 1) return false;

        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (cnpj[i] - '0') * weights1[i];

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if ((cnpj[12] - '0') != digit1) return false;

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
        CodeTextBox.Clear(); NameTextBox.Clear(); DocumentTextBox.Clear(); ContactTextBox.Clear();
        EmailTextBox.Clear(); PhoneTextBox.Clear(); ZipCodeTextBox.Clear(); CityTextBox.Clear();
        StreetTextBox.Clear(); NumberTextBox.Clear(); DistrictTextBox.Clear(); StateTextBox.Clear();
        MunicipalityComboBox.SelectedIndex = 0;
        _serviceAreas.Clear();
        ServiceAreasListBox.ItemsSource = null;
    }

    private static string? Opt(string? value)
    {
        var t = value?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    private sealed class ServiceAreaItem
    {
        public int Id { get; init; }
        public required string Display { get; init; }
    }
}
