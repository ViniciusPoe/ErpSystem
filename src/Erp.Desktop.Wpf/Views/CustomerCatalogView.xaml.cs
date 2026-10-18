using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class CustomerCatalogView : UserControl
{
    private readonly SalesApplicationService _service;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private int _selectedId;
    private bool _isBusy;
    private IReadOnlyList<Municipality> _municipalities = [];
    private IReadOnlyList<Customer> _customers = [];

    public CustomerCatalogView(SalesApplicationService service)
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
        });
    }

    private async Task LoadGridAsync()
    {
        await ViewHelper.RunSafeBusyAsync("Carregar clientes", SetBusy, async () =>
        {
            _customers = await _service.GetCustomerCatalogItemsAsync();
            ApplyFilter();
        });
    }

    private Task<string> GenerateNextCodeAsync() => _service.GetNextCustomerCodeAsync();

    private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomersGrid.SelectedItem is not Customer customer)
        {
            _selectedId = 0;
            EditButton.IsEnabled = false;
            return;
        }

        _selectedId = customer.Id;
        EditButton.IsEnabled = !_isBusy;
    }

    private async Task LoadSelectedCustomerAsync()
    {
        if (_selectedId <= 0)
            return;

        var detail = await _service.GetCustomerByIdAsync(_selectedId);
        if (detail is null)
            throw new InvalidOperationException("Cliente nao encontrado.");

        CodeTextBox.Text = detail.Code;
        NameTextBox.Text = detail.Name;
        DocumentTextBox.Text = detail.Document;
        IsActiveCheckBox.IsChecked = detail.IsActive;
        ContactTextBox.Text = detail.ContactName;
        EmailTextBox.Text = detail.Email;
        PhoneTextBox.Text = detail.Phone;
        ZipCodeTextBox.Text = detail.ZipCode;
        CityTextBox.Text = detail.City;
        StreetTextBox.Text = detail.Street;
        NumberTextBox.Text = detail.Number;
        DistrictTextBox.Text = detail.District;
        StateTextBox.Text = detail.State;

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
        await ViewHelper.RunSafeBusyAsync("Novo cliente", SetBusy, async () =>
        {
            _selectedId = 0;
            ClearFields();
            CodeTextBox.Text = await GenerateNextCodeAsync();
            CodeTextBox.IsReadOnly = true;
            CustomersGrid.SelectedItem = null;
            EditButton.IsEnabled = false;
            FormTitle.Text = "Novo Cliente";
            FormPanel.Visibility = Visibility.Visible;
            await Dispatcher.InvokeAsync(() => NameTextBox.Focus());
        });
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0)
            return;

        await ViewHelper.RunSafeBusyAsync("Carregar cliente", SetBusy, async () =>
        {
            await LoadSelectedCustomerAsync();
            CodeTextBox.IsReadOnly = true;
            FormTitle.Text = "Editar Cliente";
            FormPanel.Visibility = Visibility.Visible;
            await Dispatcher.InvokeAsync(() => NameTextBox.Focus());
        });
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        _selectedId = 0;
        ClearFields();
        CustomersGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeBusyAsync("Salvar cliente", SetBusy, async () =>
        {
            if (!ValidateForm())
                return;

            var customer = new Customer
            {
                Id = _selectedId,
                Code = CodeTextBox.Text.Trim().ToUpperInvariant(),
                Name = NameTextBox.Text.Trim(),
                Document = CleanDigits(DocumentTextBox.Text),
                IsActive = IsActiveCheckBox.IsChecked != false,
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

            if (_selectedId > 0)
            {
                await _service.UpdateCustomerAsync(customer);
            }
            else
            {
                await _service.AddCustomerAsync(
                    customer.Code,
                    customer.Name,
                    customer.Document,
                    customer.IsActive,
                    customer.ContactName,
                    customer.Email,
                    customer.Phone,
                    customer.Street,
                    customer.Number,
                    customer.District,
                    customer.City,
                    customer.State,
                    customer.ZipCode);
            }

            FormPanel.Visibility = Visibility.Collapsed;
            ClearFields();
            _selectedId = 0;
            await LoadGridAsync();
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
        else if (!code.StartsWith('C'))
            AddError(CodeTextBox, "Codigo deve comecar com 'C' (ex: C0001).");

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            AddError(NameTextBox, "Nome/Razao Social e obrigatorio.");

        var documentDigits = CleanDigits(DocumentTextBox.Text);
        if (string.IsNullOrEmpty(documentDigits))
        {
            AddError(DocumentTextBox, "CPF/CNPJ e obrigatorio.");
        }
        else if (documentDigits.Length == 11)
        {
            if (!ValidateCpf(documentDigits))
                AddError(DocumentTextBox, "CPF invalido.");
        }
        else if (documentDigits.Length == 14)
        {
            if (!ValidateCnpj(documentDigits))
                AddError(DocumentTextBox, "CNPJ invalido.");
        }
        else
        {
            AddError(DocumentTextBox, "CPF deve ter 11 digitos ou CNPJ deve ter 14 digitos.");
        }

        var email = Opt(EmailTextBox.Text);
        if (email != null && !IsValidEmail(email))
            AddError(EmailTextBox, "Email invalido. Deve conter '@' e um dominio valido.");

        var phoneDigits = CleanDigits(PhoneTextBox.Text);
        if (!string.IsNullOrEmpty(phoneDigits) && phoneDigits.Length is not (10 or 11))
            AddError(PhoneTextBox, "Telefone deve ter 10 digitos (fixo) ou 11 digitos (celular) incluindo DDD.");

        var cepDigits = CleanDigits(ZipCodeTextBox.Text);
        if (!string.IsNullOrEmpty(cepDigits) && cepDigits.Length != 8)
            AddError(ZipCodeTextBox, "CEP deve ter 8 digitos.");

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
        IEnumerable<Customer> filtered = _customers;

        if (!string.IsNullOrWhiteSpace(term))
        {
            filtered = filtered.Where(c =>
                Contains(c.Code, term) ||
                Contains(c.Name, term) ||
                Contains(c.Document, term) ||
                Contains(c.City, term) ||
                Contains(c.State, term));
        }

        CustomersGrid.ItemsSource = filtered.ToList();
    }

    private static bool Contains(string? source, string term)
        => !string.IsNullOrWhiteSpace(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool ValidateCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        int[] weights1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (cpf[i] - '0') * weights1[i];

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;
        if ((cpf[9] - '0') != digit1)
            return false;

        int[] weights2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (cpf[i] - '0') * weights2[i];

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;
        return (cpf[10] - '0') == digit2;
    }

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
        DocumentTextBox.Clear();
        ContactTextBox.Clear();
        EmailTextBox.Clear();
        PhoneTextBox.Clear();
        ZipCodeTextBox.Clear();
        CityTextBox.Clear();
        StreetTextBox.Clear();
        NumberTextBox.Clear();
        DistrictTextBox.Clear();
        StateTextBox.Clear();
        IsActiveCheckBox.IsChecked = true;
        MunicipalityComboBox.SelectedIndex = 0;
    }

    private static string? Opt(string value)
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
        CustomersGrid.IsEnabled = !isBusy;
        FormPanel.IsEnabled = !isBusy;
        MunicipalityComboBox.IsEnabled = !isBusy;
    }
}
