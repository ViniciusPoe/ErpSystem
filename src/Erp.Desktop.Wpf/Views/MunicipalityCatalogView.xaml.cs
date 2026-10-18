using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Erp.Application;
using Erp.Modules.Sales;

namespace Erp.Desktop.Wpf.Views;

public partial class MunicipalityCatalogView : UserControl
{
    private readonly SalesApplicationService _service;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private int _selectedId;
    private bool _isLoading;
    private IReadOnlyList<Municipality> _municipalities = [];
    private bool _suppressSelectionChanged;

    public MunicipalityCatalogView(SalesApplicationService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadCountries();
        await LoadGridAsync();
    }

    private async Task LoadGridAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar municipios", async () =>
        {
            _municipalities = await _service.GetMunicipalitiesAsync();
            ApplyFilter();
        });
    }

    private void ApplyFilter()
    {
        var term = SearchTextBox.Text.Trim();
        IEnumerable<Municipality> filtered = _municipalities;
        if (!string.IsNullOrWhiteSpace(term))
        {
            filtered = filtered.Where(m =>
                Contains(m.Code, term) ||
                Contains(m.Name, term) ||
                Contains(m.State, term) ||
                Contains(m.Country, term));
        }
        MunicipalitiesGrid.ItemsSource = filtered.ToList();
    }

    private static bool Contains(string? source, string term)
        => !string.IsNullOrWhiteSpace(source) && source.Contains(term, StringComparison.OrdinalIgnoreCase);

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private string GenerateNextCode()
    {
        var maxNumber = 0;
        foreach (var m in _municipalities)
        {
            if (m.Code.Length > 1 && m.Code.StartsWith('M') && int.TryParse(m.Code[1..], out var num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }
        return $"M{(maxNumber + 1):D3}";
    }

    // === Countries ===

    private static readonly string[] Countries =
    [
        "Brasil", "Argentina", "Uruguai", "Paraguai", "Chile", "Colombia", "Peru",
        "Bolivia", "Venezuela", "Equador", "Mexico", "Estados Unidos", "Canada",
        "Portugal", "Espanha", "Alemanha", "Franca", "Italia", "Reino Unido",
        "China", "Japao", "Coreia do Sul", "India", "Australia"
    ];

    private void LoadCountries()
    {
        CountryComboBox.ItemsSource = Countries;
        CountryComboBox.SelectedItem = "Brasil";
    }

    // === IBGE API for Brazil ===

    private async void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged) return;

        StateComboBox.ItemsSource = null;
        CityComboBox.ItemsSource = null;

        var country = CountryComboBox.Text?.Trim();
        if (string.Equals(country, "Brasil", StringComparison.OrdinalIgnoreCase))
        {
            await LoadBrazilianStatesAsync();
        }
    }

    private async Task LoadBrazilianStatesAsync()
    {
        await ViewHelper.RunSafeAsync("Carregar estados", async () =>
        {
            var json = await HttpClient.GetStringAsync("https://servicodados.ibge.gov.br/api/v1/localidades/estados?orderBy=nome");
            using var doc = JsonDocument.Parse(json);
            var states = doc.RootElement.EnumerateArray()
                .Select(el => el.GetProperty("sigla").GetString()!)
                .OrderBy(s => s)
                .ToList();
            StateComboBox.ItemsSource = states;
        });
    }

    private async void StateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged) return;

        CityComboBox.ItemsSource = null;

        var country = CountryComboBox.Text?.Trim();
        var state = StateComboBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(state)) return;

        if (string.Equals(country, "Brasil", StringComparison.OrdinalIgnoreCase))
        {
            await LoadBrazilianCitiesAsync(state);
        }
    }

    private async Task LoadBrazilianCitiesAsync(string uf)
    {
        await ViewHelper.RunSafeAsync("Carregar cidades", async () =>
        {
            var json = await HttpClient.GetStringAsync($"https://servicodados.ibge.gov.br/api/v1/localidades/estados/{uf}/municipios?orderBy=nome");
            using var doc = JsonDocument.Parse(json);
            var cities = doc.RootElement.EnumerateArray()
                .Select(el => el.GetProperty("nome").GetString()!)
                .OrderBy(c => c)
                .ToList();
            CityComboBox.ItemsSource = cities;
        });
    }

    // === Grid selection ===

    private void MunicipalitiesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MunicipalitiesGrid.SelectedItem is not Municipality m)
        {
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            return;
        }

        _selectedId = m.Id;
        CodeTextBox.Text = m.Code;

        _suppressSelectionChanged = true;

        // Set country
        var countryMatch = Countries.FirstOrDefault(c => string.Equals(c, m.Country, StringComparison.OrdinalIgnoreCase));
        if (countryMatch != null)
            CountryComboBox.SelectedItem = countryMatch;
        else
            CountryComboBox.Text = m.Country;

        StateComboBox.Text = m.State;
        CityComboBox.Text = m.Name;

        _suppressSelectionChanged = false;

        EditButton.IsEnabled = true;
        DeleteButton.IsEnabled = true;
    }

    // === Form actions ===

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedId = 0;
        ClearFields();
        CodeTextBox.Text = GenerateNextCode();
        MunicipalitiesGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
        FormTitle.Text = "Novo Municipio";
        FormPanel.Visibility = Visibility.Visible;

        CountryComboBox.SelectedItem = "Brasil";
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0 || _isLoading) return;

        _isLoading = true;
        EditButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;

        try
        {
            FormTitle.Text = "Editar Municipio";
            FormPanel.Visibility = Visibility.Visible;

            var country = CountryComboBox.Text?.Trim();
            if (string.Equals(country, "Brasil", StringComparison.OrdinalIgnoreCase))
            {
                await LoadBrazilianStatesAsync();
                _suppressSelectionChanged = true;
                StateComboBox.SelectedItem = _municipalities.FirstOrDefault(m => m.Id == _selectedId)?.State;
                _suppressSelectionChanged = false;

                var state = StateComboBox.SelectedItem?.ToString();
                if (!string.IsNullOrWhiteSpace(state))
                {
                    await LoadBrazilianCitiesAsync(state);
                    _suppressSelectionChanged = true;
                    CityComboBox.SelectedItem = _municipalities.FirstOrDefault(m => m.Id == _selectedId)?.Name;
                    _suppressSelectionChanged = false;
                }
            }
        }
        finally
        {
            _isLoading = false;
            EditButton.IsEnabled = _selectedId > 0;
            DeleteButton.IsEnabled = _selectedId > 0;
        }
    }

    private void CancelFormButton_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        _selectedId = 0;
        ClearFields();
        MunicipalitiesGrid.SelectedItem = null;
        EditButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewHelper.RunSafeAsync("Salvar municipio", async () =>
        {
            if (!ValidateForm()) return;

            var code = CodeTextBox.Text.Trim().ToUpperInvariant();
            var country = CountryComboBox.Text.Trim();
            var state = StateComboBox.Text.Trim();
            var city = CityComboBox.Text.Trim();

            if (_selectedId > 0)
            {
                await _service.UpdateMunicipalityAsync(new Municipality
                {
                    Id = _selectedId,
                    Code = code,
                    Name = city,
                    State = state,
                    Country = country
                });
            }
            else
            {
                await _service.AddMunicipalityAsync(code, city, state, country);
            }

            FormPanel.Visibility = Visibility.Collapsed;
            ClearFields();
            _selectedId = 0;
            await LoadGridAsync();
        });
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId <= 0) return;

        if (!ViewHelper.Confirm("Deseja realmente excluir este municipio?", "Municipios")) return;

        await ViewHelper.RunSafeAsync("Excluir municipio", async () =>
        {
            await _service.DeleteMunicipalityAsync(_selectedId);
            FormPanel.Visibility = Visibility.Collapsed;
            ClearFields();
            _selectedId = 0;
            await LoadGridAsync();
        });
    }

    private bool ValidateForm()
    {
        var errors = new List<string>();

        var code = CodeTextBox.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            errors.Add("Codigo e obrigatorio.");
        else if (!code.StartsWith('M'))
            errors.Add("Codigo deve comecar com 'M' (ex: M001).");

        if (string.IsNullOrWhiteSpace(CountryComboBox.Text))
            errors.Add("Pais e obrigatorio.");

        if (string.IsNullOrWhiteSpace(StateComboBox.Text))
            errors.Add("UF/Estado e obrigatorio.");

        if (string.IsNullOrWhiteSpace(CityComboBox.Text))
            errors.Add("Cidade e obrigatorio.");

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void ClearFields()
    {
        _suppressSelectionChanged = true;
        CodeTextBox.Clear();
        CountryComboBox.SelectedItem = "Brasil";
        StateComboBox.ItemsSource = null;
        StateComboBox.Text = string.Empty;
        CityComboBox.ItemsSource = null;
        CityComboBox.Text = string.Empty;
        _suppressSelectionChanged = false;
    }
}
