using System.Globalization;
using System.Windows;
using Erp.Desktop.Wpf.Diagnostics;

namespace Erp.Desktop.Wpf.Views;

internal static class ViewHelper
{
    public static async Task RunSafeAsync(string op, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            TerminalDebug.Error(op, ex);
            MessageBox.Show(BuildUserMessage(ex), op, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static async Task RunSafeBusyAsync(string op, Action<bool> setBusy, Func<Task> action)
    {
        setBusy(true);
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            TerminalDebug.Error(op, ex);
            MessageBox.Show(BuildUserMessage(ex), op, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            setBusy(false);
        }
    }

    public static void RunSafe(string op, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            TerminalDebug.Error(op, ex);
            MessageBox.Show(BuildUserMessage(ex), op, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static bool TryDecimal(string raw, out decimal value)
        => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
        || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    public static decimal ReadDecimal(string raw) => TryDecimal(raw, out var value) ? value : 0m;

    public static bool Confirm(string message, string title = "Confirmar")
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static string PaymentTermFromComboBox(System.Windows.Controls.ComboBox comboBox, string fallback = "A_VISTA")
    {
        var item = comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
        var value = item?.Tag?.ToString() ?? item?.Content?.ToString();
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string BuildUserMessage(Exception ex)
    {
        if (ex is InvalidOperationException or ArgumentException)
            return ex.Message;

        if (ex.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
            return "Ja existe um cadastro com um dos identificadores informados. Revise codigo, documento, SKU, CNPJ ou codigo de barras.";

        return "Ocorreu um erro ao executar a operacao. Revise os dados informados e tente novamente.";
    }
}
