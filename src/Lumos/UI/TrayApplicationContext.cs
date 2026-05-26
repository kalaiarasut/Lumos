using System.Drawing;
using System.Diagnostics;
using Lumos.Services;
using Lumos.Services.Interfaces;

namespace Lumos.UI;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IActiveWindowService _activeWindowService;
    private readonly AutomationCoordinator? _coordinator;
    private readonly IBrightnessProvider? _brightnessProvider;
    private readonly ILoggingService _loggingService;
    private readonly NotifyIcon _notifyIcon;
    private readonly IProfileStore _profileStore;
    private readonly IThemeService _themeService;
    private readonly IClock _clock;
    private readonly StartupService _startupService;
    private readonly string _dataRoot;
    private readonly string _logPath;
    private readonly System.Windows.Forms.Timer _foregroundTimer;
    private readonly System.Windows.Forms.Timer _brightnessTimer;
    private readonly ToolStripMenuItem _automationMenuItem;
    private readonly ToolStripMenuItem _pauseMenuItem;
    private readonly ToolStripMenuItem _resumeMenuItem;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _currentAppMenuItem;
    private readonly ToolStripMenuItem _currentBrightnessMenuItem;
    private readonly ToolStripMenuItem _providerMenuItem;

    public TrayApplicationContext(
        IProfileStore profileStore,
        IThemeService themeService,
        IClock clock,
        StartupService startupService,
        IActiveWindowService activeWindowService,
        ILoggingService loggingService,
        string dataRoot,
        string logPath,
        AutomationCoordinator? coordinator,
        IBrightnessProvider? brightnessProvider)
    {
        _profileStore = profileStore;
        _themeService = themeService;
        _clock = clock;
        _startupService = startupService;
        _activeWindowService = activeWindowService;
        _loggingService = loggingService;
        _dataRoot = dataRoot;
        _logPath = logPath;
        _coordinator = coordinator;
        _brightnessProvider = brightnessProvider;

        _automationMenuItem = new ToolStripMenuItem("Automation Enabled") { CheckOnClick = true };
        _automationMenuItem.Click += async (_, _) => await ToggleAutomationAsync();

        _pauseMenuItem = new ToolStripMenuItem("Pause for 30 Minutes");
        _pauseMenuItem.Click += async (_, _) => await PauseForThirtyMinutesAsync();

        _resumeMenuItem = new ToolStripMenuItem("Resume Now");
        _resumeMenuItem.Click += async (_, _) => await ResumeNowAsync();

        _statusMenuItem = new ToolStripMenuItem("Status: starting") { Enabled = false };
        _currentAppMenuItem = new ToolStripMenuItem("Active App: -") { Enabled = false };
        _currentBrightnessMenuItem = new ToolStripMenuItem("Brightness: -") { Enabled = false };
        _providerMenuItem = new ToolStripMenuItem($"Provider: {brightnessProvider?.ProviderName.ToUpperInvariant() ?? "Unavailable"}") { Enabled = false };

        _notifyIcon = new NotifyIcon
        {
            Text = "Lumos",
            Visible = true,
            Icon = SystemIcons.Application,
            ContextMenuStrip = BuildMenu(),
        };

        _foregroundTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _foregroundTimer.Tick += async (_, _) => await OnForegroundTickAsync();
        _foregroundTimer.Start();

        _brightnessTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _brightnessTimer.Tick += async (_, _) => await OnBrightnessTickAsync();
        _brightnessTimer.Start();

        _ = InitializeMenuStateAsync();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(_automationMenuItem);
        menu.Items.Add(_pauseMenuItem);
        menu.Items.Add(_resumeMenuItem);
        menu.Items.Add("Disable Automation", null, async (_, _) => await DisableAutomationAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(_currentAppMenuItem);
        menu.Items.Add(_currentBrightnessMenuItem);
        menu.Items.Add(_providerMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Profiles", null, async (_, _) => await ShowProfilesAsync());
        menu.Items.Add("Settings", null, async (_, _) => await ShowSettingsAsync());
        menu.Items.Add("Forget Current App", null, async (_, _) => await ForgetCurrentAppAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Test Brightness Control", null, async (_, _) => await TestBrightnessControlAsync());
        menu.Items.Add("Open Logs", null, (_, _) => OpenPath(_logPath));
        menu.Items.Add("Open Data Folder", null, (_, _) => OpenPath(_dataRoot));
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        return menu;
    }

    private async Task InitializeMenuStateAsync()
    {
        try
        {
            var settings = await _profileStore.LoadSettingsAsync();
            _automationMenuItem.Checked = settings.AutomationEnabled;
            UpdateStatus(settings);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to initialize tray menu state.", ex);
        }
    }

    private async Task ToggleAutomationAsync()
    {
        try
        {
            var settings = await _profileStore.LoadSettingsAsync();
            settings.AutomationEnabled = _automationMenuItem.Checked;
            await _profileStore.SaveSettingsAsync(settings);
            UpdateStatus(settings);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to toggle automation.", ex);
        }
    }

    private async Task PauseForThirtyMinutesAsync()
    {
        try
        {
            var settings = await _profileStore.LoadSettingsAsync();
            settings.PauseUntilUtc = _clock.UtcNow.AddMinutes(30);
            await _profileStore.SaveSettingsAsync(settings);
            UpdateStatus(settings);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to pause automation.", ex);
        }
    }

    private async Task ResumeNowAsync()
    {
        try
        {
            var settings = await _profileStore.LoadSettingsAsync();
            settings.PauseUntilUtc = null;
            settings.AutomationEnabled = true;
            await _profileStore.SaveSettingsAsync(settings);
            _automationMenuItem.Checked = true;
            UpdateStatus(settings);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to resume automation.", ex);
        }
    }

    private async Task DisableAutomationAsync()
    {
        try
        {
            var settings = await _profileStore.LoadSettingsAsync();
            settings.AutomationEnabled = false;
            settings.PauseUntilUtc = null;
            await _profileStore.SaveSettingsAsync(settings);
            _automationMenuItem.Checked = false;
            UpdateStatus(settings);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to disable automation.", ex);
        }
    }

    private async Task ShowSettingsAsync()
    {
        try
        {
            var settings = await _profileStore.LoadSettingsAsync();
            using var form = new SettingsForm(settings, _themeService);
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var updated = form.BuildUpdatedSettings(settings);
            await _profileStore.SaveSettingsAsync(updated);
            _automationMenuItem.Checked = updated.AutomationEnabled;
            _startupService.SetEnabled(Application.ExecutablePath, updated.StartupEnabled);
            UpdateStatus(updated);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to show or save settings.", ex);
        }
    }

    private async Task ForgetCurrentAppAsync()
    {
        try
        {
            var currentExe = _activeWindowService.GetForegroundExecutableName();
            if (string.IsNullOrWhiteSpace(currentExe))
            {
                return;
            }

            var profiles = await _profileStore.LoadProfilesAsync();
            var removed = profiles.RemoveAll(profile => string.Equals(profile.ExeName, currentExe, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                await _profileStore.SaveProfilesAsync(profiles);
                _loggingService.Info($"Forgot profile for {currentExe}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to forget current app profile.", ex);
        }
    }

    private async Task ShowProfilesAsync()
    {
        try
        {
            var profiles = await _profileStore.LoadProfilesAsync();
            var sets = await _profileStore.LoadProfileSetsAsync();

            using var form = new ProfilesForm(profiles, sets, _brightnessProvider, _themeService);
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            await _profileStore.SaveProfilesAsync(form.GetProfiles());
            if (form.PendingSavedProfileSet is not null)
            {
                await _profileStore.SaveProfileSetAsync(form.PendingSavedProfileSet);
            }
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to show or save profiles.", ex);
        }
    }

    private async Task OnForegroundTickAsync()
    {
        try
        {
            if (_coordinator is null)
            {
                _currentAppMenuItem.Text = "Active App: no brightness provider";
                return;
            }

            await _coordinator.TickAsync();
            var exeName = _activeWindowService.GetForegroundExecutableName() ?? "-";
            _currentAppMenuItem.Text = $"Active App: {exeName}";
            UpdateStatus(await _profileStore.LoadSettingsAsync());
        }
        catch (Exception ex)
        {
            _currentAppMenuItem.Text = "Active App: error";
            _loggingService.Error("Foreground polling failed.", ex);
        }
    }

    private async Task OnBrightnessTickAsync()
    {
        try
        {
            if (_coordinator is null || _brightnessProvider is null)
            {
                _currentBrightnessMenuItem.Text = "Brightness: unavailable";
                return;
            }

            var brightness = await _brightnessProvider.GetCurrentBrightnessAsync();
            _currentBrightnessMenuItem.Text = $"Brightness: {brightness}%";
            await _coordinator.RecordObservedBrightnessAsync(brightness);
            await _coordinator.FlushPendingLearningAsync();
        }
        catch (Exception ex)
        {
            _currentBrightnessMenuItem.Text = "Brightness: error";
            _loggingService.Error("Brightness polling failed.", ex);
        }
    }

    private async Task TestBrightnessControlAsync()
    {
        try
        {
            if (_brightnessProvider is null)
            {
                MessageBox.Show("No supported brightness provider is available.", "Lumos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var brightness = await _brightnessProvider.GetCurrentBrightnessAsync();
            await _brightnessProvider.SetBrightnessAsync(brightness);
            MessageBox.Show($"Brightness control works. Current brightness: {brightness}%.", "Lumos", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Brightness control test failed.", ex);
            MessageBox.Show($"Brightness control failed: {ex.Message}", "Lumos", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateStatus(Models.AppSettings settings)
    {
        var status = "Running";
        if (_brightnessProvider is null)
        {
            status = "No brightness provider";
        }
        else if (!settings.AutomationEnabled)
        {
            status = "Disabled";
        }
        else if (settings.PauseUntilUtc is not null && settings.PauseUntilUtc > _clock.UtcNow)
        {
            status = "Paused";
        }

        _pauseMenuItem.Text = status == "Paused" ? "Paused for 30 Minutes" : "Pause for 30 Minutes";
        _resumeMenuItem.Enabled = status == "Paused" || status == "Disabled";
        _statusMenuItem.Text = $"Status: {status}";
        _notifyIcon.Text = $"Lumos - {status}";
    }

    private void OpenPath(string path)
    {
        try
        {
            if (string.Equals(path, _logPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, string.Empty);
                }
            }

            if (string.Equals(path, _dataRoot, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to open path: {path}", ex);
        }
    }

    protected override void ExitThreadCore()
    {
        _foregroundTimer.Stop();
        _foregroundTimer.Dispose();
        _brightnessTimer.Stop();
        _brightnessTimer.Dispose();
        _coordinator?.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
