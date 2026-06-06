using Microsoft.Win32;
using Lumos.Models;

namespace Lumos.Services;

public static class InstalledAppCatalog
{
    private static readonly string[] UninstallKeyPaths =
    [
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
        @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    public static IReadOnlyList<InstalledAppOption> GetInstalledApps()
    {
        var apps = new Dictionary<string, InstalledAppOption>(StringComparer.OrdinalIgnoreCase);

        AddFromAppPaths(apps, Registry.CurrentUser);
        AddFromAppPaths(apps, Registry.LocalMachine);
        AddFromUninstallKeys(apps, Registry.CurrentUser);
        AddFromUninstallKeys(apps, Registry.LocalMachine);

        return apps.Values
            .OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(app => app.ExeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddFromAppPaths(Dictionary<string, InstalledAppOption> apps, RegistryKey root)
    {
        using var appPaths = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");
        if (appPaths is null)
        {
            return;
        }

        foreach (var subKeyName in appPaths.GetSubKeyNames())
        {
            if (!subKeyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var appKey = appPaths.OpenSubKey(subKeyName);
            var path = appKey?.GetValue(null) as string;
            var exeName = Path.GetFileName(string.IsNullOrWhiteSpace(path) ? subKeyName : path);
            if (!IsUsableExeName(exeName))
            {
                continue;
            }

            AddApp(apps, new InstalledAppOption
            {
                DisplayName = ToDisplayName(Path.GetFileNameWithoutExtension(exeName)),
                ExeName = exeName,
                SourcePath = path
            });
        }
    }

    private static void AddFromUninstallKeys(Dictionary<string, InstalledAppOption> apps, RegistryKey root)
    {
        foreach (var keyPath in UninstallKeyPaths)
        {
            using var uninstallKey = root.OpenSubKey(keyPath);
            if (uninstallKey is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var appKey = uninstallKey.OpenSubKey(subKeyName);
                var displayName = appKey?.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var exePath = TryGetExecutableFromDisplayIcon(appKey?.GetValue("DisplayIcon") as string)
                    ?? TryFindLikelyExecutable(appKey?.GetValue("InstallLocation") as string, displayName);
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    continue;
                }

                var exeName = Path.GetFileName(exePath);
                if (!IsUsableExeName(exeName))
                {
                    continue;
                }

                AddApp(apps, new InstalledAppOption
                {
                    DisplayName = displayName.Trim(),
                    ExeName = exeName,
                    SourcePath = exePath
                });
            }
        }
    }

    private static string? TryGetExecutableFromDisplayIcon(string? displayIcon)
    {
        if (string.IsNullOrWhiteSpace(displayIcon))
        {
            return null;
        }

        var text = displayIcon.Trim();
        if (text.StartsWith('"'))
        {
            var closingQuote = text.IndexOf('"', 1);
            if (closingQuote > 1)
            {
                text = text[1..closingQuote];
            }
        }
        else
        {
            var exeIndex = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIndex >= 0)
            {
                text = text[..(exeIndex + 4)];
            }
        }

        return text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? text : null;
    }

    private static string? TryFindLikelyExecutable(string? installLocation, string displayName)
    {
        if (string.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
        {
            return null;
        }

        try
        {
            var displayTokens = GetSearchTokens(displayName);
            var executables = Directory.EnumerateFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly)
                .Where(path => !IsHelperExecutable(Path.GetFileName(path)))
                .Take(30)
                .ToList();

            return executables.FirstOrDefault(path =>
                displayTokens.Any(token => Path.GetFileNameWithoutExtension(path).Contains(token, StringComparison.OrdinalIgnoreCase)))
                ?? executables.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> GetSearchTokens(string displayName)
    {
        return displayName
            .Split([' ', '-', '_', '.', '(', ')'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 3);
    }

    private static bool IsHelperExecutable(string exeName)
    {
        var name = Path.GetFileNameWithoutExtension(exeName);
        return name.StartsWith("unins", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("uninstall", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("setup", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("update", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("crash", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUsableExeName(string? exeName)
    {
        return !string.IsNullOrWhiteSpace(exeName) &&
               exeName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
               !IsHelperExecutable(exeName);
    }

    private static void AddApp(Dictionary<string, InstalledAppOption> apps, InstalledAppOption app)
    {
        var key = app.ExeName;
        if (!apps.ContainsKey(key))
        {
            apps[key] = app;
        }
    }

    private static string ToDisplayName(string name)
    {
        return string.Join(' ', name.Split(['-', '_', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
