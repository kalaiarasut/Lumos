using Lumos.Services;
using Lumos.UI;

namespace Lumos;

public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--shutdown", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownSignal.RequestShutdown();
            return;
        }

        using var singleInstanceGuard = new SingleInstanceGuard();
        if (!singleInstanceGuard.IsOwner)
        {
            MessageBox.Show("Lumos is already running.", "Lumos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var shutdownSignal = new ShutdownSignal();

        ApplicationConfiguration.Initialize();

        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumos");
        var logPath = Path.Combine(dataRoot, "logs", "lumos.log");
        var profileStore = new ProfileStore(dataRoot);

        var loggingService = new LoggingService(logPath);
        var clock = new SystemClock();
        var startupService = new StartupService();
        var activeWindowService = new ActiveWindowService();
        var settings = profileStore.LoadSettingsAsync().GetAwaiter().GetResult();
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
            settings.FirstRunCompleted = true;
            settings.StartupEnabled = true;
            profileStore.SaveSettingsAsync(settings).GetAwaiter().GetResult();
            startupService.SetEnabled(Application.ExecutablePath, settings.StartupEnabled);
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
            clock,
            startupService,
            activeWindowService,
            loggingService,
            dataRoot,
            logPath,
            coordinator,
            brightnessProviderManager.ActiveProvider,
            shutdownSignal));
    }
}
