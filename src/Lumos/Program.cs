using Lumos.Services;
using Lumos.UI;

namespace Lumos;

public static class Program
{
    [STAThread]
    private static async Task Main()
    {
        ApplicationConfiguration.Initialize();

        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumos");
        var profileStore = new ProfileStore(dataRoot);
        var themeService = new ThemeService();
        var loggingService = new LoggingService(Path.Combine(dataRoot, "logs", "lumos.log"));
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

        Application.Run(new TrayApplicationContext(
            profileStore,
            themeService,
            clock,
            startupService,
            activeWindowService,
            coordinator,
            brightnessProviderManager.ActiveProvider));
    }
}
