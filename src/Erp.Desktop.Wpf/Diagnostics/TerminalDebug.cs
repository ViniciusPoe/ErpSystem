using System.Text.RegularExpressions;

namespace Erp.Desktop.Wpf.Diagnostics;

public static class TerminalDebug
{
    private static readonly object Sync = new();

    public static void Info(string message)
    {
        Write("INFO", message, null);
    }

    public static void Error(string message, Exception exception)
    {
        Write("ERROR", message, exception);
    }

    private static void Write(string level, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] [{level}] {message}";

        lock (Sync)
        {
            if (level == "ERROR")
            {
                Console.Error.WriteLine(line);
                if (exception is not null)
                    Console.Error.WriteLine($"{exception.GetType().Name}: {Sanitize(exception.Message)}");
            }
            else
            {
                Console.WriteLine(line);
            }
        }
    }

    private static string Sanitize(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Erro interno.";

        var sanitized = Regex.Replace(message, @"'[^']+'", "'***'");
        sanitized = Regex.Replace(sanitized, @"\b\d{8,}\b", "***");
        sanitized = Regex.Replace(sanitized, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", "***@***", RegexOptions.IgnoreCase);
        return sanitized;
    }
}
