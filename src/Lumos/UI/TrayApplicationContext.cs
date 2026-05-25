using System.Drawing;
using Lumos.Services;
using Lumos.Services.Interfaces;

namespace Lumos.UI;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IActiveWindowService _activeWindowService;
    private readonly AutomationCoordinator? _coordinator;
    private readonly IBrightnessProvider? _brightnessProvider;
    private readonly NotifyIcon _notifyIcon;
    private readonly IProfileStore _profileStore;
    private readonly IThemeService _themeService;
    private readonly IClock _clock;
    private readonly StartupService _startupService;
    private readonly Timer _foregroundTimer;
    private readonly Timer _brightnessTimer;
    private readonly ToolStripMenuItem _automationMenuItem;
    private readonly ToolStripMenuItem _pauseMenuItem;
    private readonly ToolStripMenuItem _currentAppMenuItem;
    private readonly ToolStripMenuItem _currentBrightnessMenuItem;

    public TrayApplicationContext(
        IProfileStore profileStore,
        IThemeService themeService,
        IClock clock,
        StartupService startupService,
        IActiveWindowService activeWindowService,
        AutomationCoordinator? coordinator,
        IBrightnessProvider? brightnessProvider)
    {
        _profileStore = profileStore;
        _themeService = themeService;
        _clock = clock;
        _startupService = startupService;
        _activeWindowService = activeWindowService;
        _coordinator = coordinator;
        _brightnessProvider = brightnessProvider;

        _automationMenuItem = new ToolStripMenuItem("Automation Enabled") { CheckOnClick = true };
        _automationMenuItem.Click += async (_, _) => await ToggleAutomationAsync();

        _pauseMenuItem = new ToolStripMenuItem("Pause for 30 Minutes");
        _pauseMenuItem.Click += async (_, _) => await PauseForThirtyMinutesAsync();

        _currentAppMenuItem = new ToolStripMenuItem("Active App: -") { Enabled = false };
        _currentBrightnessMenuItem = new ToolStripMenuItem("Brightness: -") { Enabled = false };

        _notifyIcon = new NotifyIcon
        {
            Text = "Lumos",
            Visible = true,
            Icon = SystemIcons.Application,
            ContextMenuStrip = BuildMenu(),
        };

        _foregroundTimer = new Timer { Interval = 300 };
        _foregroundTimer.Tick += async (_, _) => await OnForegroundTickAsync();
        _foregroundTimer.Start();

        _brightnessTimer = new Timer { Interval = 500 };
        _brightnessTimer.Tick += async (_, _) => await OnBrightnessTickAsync();
        _brightnessTimer.Start();

        _ = InitializeMenuStateAsync();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(_automationMenuItem);
        menu.Items.Add(_pauseMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_currentAppMenuItem);
        menu.Items.Add(_currentBrightnessMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Profiles", null, async (_, _) => await ShowProfilesAsync());
        menu.Items.Add("Settings", null, async (_, _) => await ShowSettingsAsync());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        return menu;
    }

    private async Task InitializeMenuStateAsync()
    {
        var settings = await _profileStore.LoadSettingsAsync();
        _automationMenuItem.Checked = settings.AutomationEnabled;
    }

    private async Task ToggleAutomationAsync()
    {
        var settings = await _profileStore.LoadSettingsAsync();
        settings.AutomationEnabled = _automationMenuItem.Checked;
        await _profileStore.SaveSettingsAsync(settings);
    }

    private async Task PauseForThirtyMinutesAsync()
    {
        var settings = await _profileStore.LoadSettingsAsync();
        settings.PauseUntilUtc = _clock.UtcNow.AddMinutes(30);
        await _profileStore.SaveSettingsAsync(settings);
        _pauseMenuItem.Text = "Paused for 30 Minutes";
    }

    private async Task ShowSettingsAsync()
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
    }

    private async Task ShowProfilesAsync()
    {
        var profiles = await _profileStore.LoadProfilesAsync();
        var sets = await _profileStore.LoadProfileSetsAsync();

        using var form = new ProfilesForm(profiles, sets, _themeService);
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

    private async Task OnForegroundTickAsync()
    {
        if (_coordinator is null)
        {
            _currentAppMenuItem.Text = "Active App: no brightness provider";
            return;
        }

        await _coordinator.TickAsync();
        var exeName = _activeWindowService.GetForegroundExecutableName() ?? "-";
        _currentAppMenuItem.Text = $"Active App: {exeName}";
    }

    private async Task OnBrightnessTickAsync()
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

    protected override void ExitThreadCore()
    {
        _foregroundTimer.Stop();
        _foregroundTimer.Dispose();
        _brightnessTimer.Stop();
        _brightnessTimer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
