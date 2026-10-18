using System;
using System.Collections.Generic;
using System.IO;

namespace Erp.Infrastructure.MySql.Config;

public static class MySqlSettings
{
    public static string GetConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ERP_MYSQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var fromDotEnv = TryReadFromDotEnv("ERP_MYSQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromDotEnv))
        {
            return fromDotEnv;
        }

        throw new InvalidOperationException("Defina ERP_MYSQL_CONNECTION no ambiente ou no arquivo .env.");
    }

    private static string? TryReadFromDotEnv(string key)
    {
        foreach (var candidatePath in EnumerateDotEnvCandidates())
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            foreach (var line in File.ReadAllLines(candidatePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var equalIndex = trimmed.IndexOf('=');
                if (equalIndex <= 0)
                {
                    continue;
                }

                var currentKey = trimmed[..equalIndex].Trim();
                if (!string.Equals(currentKey, key, StringComparison.Ordinal))
                {
                    continue;
                }

                var rawValue = trimmed[(equalIndex + 1)..].Trim();
                if (rawValue.StartsWith('"') && rawValue.EndsWith('"') && rawValue.Length >= 2)
                {
                    rawValue = rawValue[1..^1];
                }

                return rawValue;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateDotEnvCandidates()
    {
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var currentDirectory = Directory.GetCurrentDirectory();
        foreach (var path in EnumerateDirectoryTree(currentDirectory))
        {
            if (yielded.Add(path))
            {
                yield return path;
            }
        }

        var appBaseDirectory = AppContext.BaseDirectory;
        foreach (var path in EnumerateDirectoryTree(appBaseDirectory))
        {
            if (yielded.Add(path))
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> EnumerateDirectoryTree(string startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            yield break;
        }

        var current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            yield return Path.Combine(current.FullName, ".env");
            current = current.Parent;
        }
    }
}
