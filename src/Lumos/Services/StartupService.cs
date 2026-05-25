using Microsoft.Win32;

namespace Lumos.Services;

public sealed class StartupService
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Lumos";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(AppName) is string;
    }

    public void SetEnabled(string executablePath, bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(AppName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
