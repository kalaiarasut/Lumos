using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lumos.UI.WPF;

public sealed class ExeIconConverter : IValueConverter
{
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ImageSource GenericIcon = CreateImageSource(SystemIcons.Application);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string exeName || string.IsNullOrWhiteSpace(exeName))
        {
            return null;
        }

        if (Cache.TryGetValue(exeName, out var cached))
        {
            return cached;
        }

        var icon = TryLoadIcon(exeName) ?? GenericIcon;
        Cache[exeName] = icon;
        return icon;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;

    private static ImageSource? TryLoadIcon(string exeName)
    {
        var path = TryResolveExecutablePath(exeName);
        if (path is null)
        {
            return null;
        }

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null)
            {
                return null;
            }

            return CreateImageSource(icon);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveExecutablePath(string exeName)
    {
        if (exeName.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
        {
            var explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            return File.Exists(explorerPath) ? explorerPath : null;
        }

        var appPath = TryResolveFromAppPaths(exeName);
        if (appPath is not null)
        {
            return appPath;
        }

        var processName = Path.GetFileNameWithoutExtension(exeName);
        try
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                using (process)
                {
                    try
                    {
                        var path = process.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        {
                            return path;
                        }
                    }
                    catch
                    {
                        // Some elevated/system processes block MainModule access.
                    }
                }
            }
        }
        catch
        {
            return null;
        }

        return TryResolveFromPath(exeName) ??
               TryResolveKnownInstallPath(exeName) ??
               TryResolveFromCommonInstallFolders(exeName);
    }

    private static ImageSource CreateImageSource(Icon icon)
    {
        var image = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(32, 32));
        image.Freeze();
        return image;
    }

    private static string? TryResolveFromAppPaths(string exeName)
    {
        var subKey = $@"Software\Microsoft\Windows\CurrentVersion\App Paths\{exeName}";
        var roots = new[]
        {
            Registry.CurrentUser,
            Registry.LocalMachine,
        };

        foreach (var root in roots)
        {
            using var key = root.OpenSubKey(subKey);
            var path = key?.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return path;
            }
        }

        using var wowKey = Registry.LocalMachine.OpenSubKey($@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
        var wowPath = wowKey?.GetValue(null) as string;
        return !string.IsNullOrWhiteSpace(wowPath) && File.Exists(wowPath) ? wowPath : null;
    }

    private static string? TryResolveFromPath(string exeName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var candidate = Path.Combine(directory, exeName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore invalid PATH entries.
            }
        }

        return null;
    }

    private static string? TryResolveKnownInstallPath(string exeName)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var candidates = new[]
        {
            Path.Combine(programFiles, "VideoLAN", "VLC", exeName),
            Path.Combine(programFilesX86, "VideoLAN", "VLC", exeName),
            Path.Combine(programFiles, "Google", "Chrome", "Application", exeName),
            Path.Combine(programFilesX86, "Google", "Chrome", "Application", exeName),
            Path.Combine(localAppData, "Programs", "Microsoft VS Code", exeName),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? TryResolveFromCommonInstallFolders(string exeName)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
        }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            var found = TryFindExecutable(root, exeName, maxDirectories: 2500);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static string? TryFindExecutable(string root, string exeName, int maxDirectories)
    {
        var queue = new Queue<string>();
        queue.Enqueue(root);
        var visited = 0;

        while (queue.Count > 0 && visited++ < maxDirectories)
        {
            var directory = queue.Dequeue();
            try
            {
                var candidate = Path.Combine(directory, exeName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                foreach (var child in Directory.EnumerateDirectories(directory))
                {
                    queue.Enqueue(child);
                }
            }
            catch
            {
                // Ignore inaccessible install directories.
            }
        }

        return null;
    }
}
