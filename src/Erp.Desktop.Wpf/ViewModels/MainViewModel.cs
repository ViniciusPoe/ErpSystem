using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Erp.Application;
using Erp.Desktop.Wpf.Commands;
using Erp.Domain;

namespace Erp.Desktop.Wpf.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<ModuleOverview> _modules;
    private ModuleOverview? _selectedModule;

    public MainViewModel()
    {
        var moduleService = new ModuleOverviewService();
        _modules = new ObservableCollection<ModuleOverview>(moduleService.BuildInitialPlan());
        SelectModuleCommand = new RelayCommand(SelectModule);

        SelectedModule = _modules.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ModuleOverview> Modules => _modules;

    public ModuleOverview? SelectedModule
    {
        get => _selectedModule;
        private set
        {
            if (_selectedModule == value)
            {
                return;
            }

            _selectedModule = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSalesModuleSelected));
        }
    }

    public bool IsSalesModuleSelected => SelectedModule?.Module == ErpModule.Sales;

    public ICommand SelectModuleCommand { get; }

    private void SelectModule(object? parameter)
    {
        if (parameter is ModuleOverview module)
        {
            SelectedModule = module;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
