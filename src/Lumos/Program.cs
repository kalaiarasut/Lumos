using Lumos.Services;
using Lumos.UI;

namespace Lumos;

public static class Program
{
    [STAThread]
    private static async Task Main()
    {
        using var singleInstanceGuard = new SingleInstanceGuard();
        if (!singleInstanceGuard.IsOwner)
        {
            MessageBox.Show("Lumos is already running.", "Lumos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumos");
        var logPath = Path.Combine(dataRoot, "logs", "lumos.log");
        var profileStore = new ProfileStore(dataRoot);
        var themeService = new ThemeService();
        var loggingService = new LoggingService(logPath);
        var clock = new SystemClock();
        var startupService = new StartupService();
        var activeWindowService = new ActiveWindowService();
        var settings = await profileStore.LoadSettingsAsync();
        var brightnessProviderManager = new BrightnessProviderManager([new WmiBrightnessProvider()]);

        AutomationCoordinator? coordinator = null;
        if (brightnessProviderManager.ActiveProvider is not null)
        {
            var transitionService = new TransitionService(brightnessProviderManager.ActiveProvider);
            coordinator = new AutomationCoordinator(
                activeWindowService,
                brightnessProviderManager.ActiveProvider,
                profileStore,
                clock,
                loggingService,
                transitionService);
        }

        if (!settings.FirstRunCompleted)
        {
            using var form = new FirstRunForm(themeService);
            if (form.ShowDialog() == DialogResult.OK)
            {
                settings.FirstRunCompleted = true;
                settings.StartupEnabled = form.StartWithWindows;
                await profileStore.SaveSettingsAsync(settings);
                startupService.SetEnabled(Application.ExecutablePath, settings.StartupEnabled);
            }
            else
            {
                return;
            }
        }
        else if (settings.StartupEnabled)
        {
            startupService.SetEnabled(Application.ExecutablePath, true);
        }

        loggingService.Info(brightnessProviderManager.ActiveProvider is null
            ? "No supported brightness provider was detected. Tray UI will remain available."
            : $"Using brightness provider: {brightnessProviderManager.ActiveProvider.ProviderName}");

        Application.Run(new TrayApplicationContext(
            profileStore,
            themeService,
            clock,
            startupService,
            activeWindowService,
            loggingService,
            dataRoot,
            logPath,
            coordinator,
            brightnessProviderManager.ActiveProvider));
    }
}
