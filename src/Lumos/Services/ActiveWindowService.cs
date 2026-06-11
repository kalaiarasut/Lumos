using System.Diagnostics;
using System.Runtime.InteropServices;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed partial class ActiveWindowService : IActiveWindowService
{
    private static readonly HashSet<string> IgnoredExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "SearchHost.exe",
        "ApplicationFrameHost.exe",
        "Lumos.exe",
    };

    public string? GetForegroundExecutableName()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName + ".exe";
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyCollection<string> GetRunningExecutableNames()
    {
        var executableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(process.ProcessName))
                    {
                        continue;
                    }

                    var exeName = process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? process.ProcessName
                        : process.ProcessName + ".exe";

                    if (!IsIgnoredExecutable(exeName))
                    {
                        executableNames.Add(exeName);
                    }
                }
                catch
                {
                    // Some protected/system processes can disappear or deny access while enumerating.
                }
            }
        }

        return executableNames;
    }

    public static bool IsIgnoredExecutable(string exeName) => IgnoredExecutables.Contains(exeName);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
