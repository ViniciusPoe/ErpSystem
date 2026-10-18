using System.Windows;
using Erp.Desktop.Wpf.Diagnostics;

namespace Erp.Desktop.Wpf;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        TerminalDebug.Info("Aplicacao iniciada.");
        base.OnStartup(e);
    }

    private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        TerminalDebug.Error("Excecao nao tratada na UI.", e.Exception);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            TerminalDebug.Error("Excecao nao tratada no AppDomain.", ex);
            return;
        }

        TerminalDebug.Info("Excecao nao tratada no AppDomain sem details de Exception.");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        TerminalDebug.Error("Task nao observada.", e.Exception);
        e.SetObserved();
    }
}
