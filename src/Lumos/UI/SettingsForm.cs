using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.UI;

public sealed class SettingsForm : Form
{
    private readonly CheckBox _automationCheckbox;
    private readonly CheckBox _startupCheckbox;
    private readonly CheckBox _transitionsCheckbox;
    private readonly CheckBox _thresholdCheckbox;
    private readonly NumericUpDown _thresholdValue;
    private readonly NumericUpDown _cooldownValue;
    private readonly Button _saveButton;

    public SettingsForm(AppSettings settings, IThemeService? themeService = null)
    {
        Text = "Lumos Settings";
        Width = 460;
        Height = 390;
        StartPosition = FormStartPosition.CenterScreen;

        var themeLabel = new Label
        {
            Left = 20,
            Top = 20,
            Width = 220,
            Text = "Theme: dark (light later)",
        };

        _automationCheckbox = new CheckBox
        {
            Left = 20,
            Top = 60,
            Width = 220,
            Text = "Enable automation",
            Checked = settings.AutomationEnabled,
        };

        _startupCheckbox = new CheckBox
        {
            Left = 20,
            Top = 95,
            Width = 220,
            Text = "Start with Windows",
            Checked = settings.StartupEnabled,
        };

        _transitionsCheckbox = new CheckBox
        {
            Left = 20,
            Top = 130,
            Width = 220,
            Text = "Enable smooth transitions",
            Checked = settings.TransitionsEnabled,
        };

        _thresholdCheckbox = new CheckBox
        {
            Left = 20,
            Top = 165,
            Width = 260,
            Text = "Skip tiny brightness differences",
            Checked = settings.SkipSmallBrightnessDifferences,
        };

        _thresholdValue = new NumericUpDown
        {
            Left = 40,
            Top = 200,
            Width = 80,
            Minimum = 0,
            Maximum = 20,
            Value = settings.SmallDifferenceThreshold,
        };

        var cooldownLabel = new Label
        {
            Left = 20,
            Top = 235,
            Width = 260,
            Text = "Manual restore cooldown (seconds)",
        };

        _cooldownValue = new NumericUpDown
        {
            Left = 40,
            Top = 260,
            Width = 80,
            Minimum = 0,
            Maximum = 60,
            Value = settings.ManualChangeRestoreCooldownSeconds,
        };

        _saveButton = new Button
        {
            Left = 320,
            Top = 310,
            Width = 100,
            Height = 32,
            Text = "Save",
            DialogResult = DialogResult.OK,
        };

        Controls.Add(themeLabel);
        Controls.Add(_automationCheckbox);
        Controls.Add(_startupCheckbox);
        Controls.Add(_transitionsCheckbox);
        Controls.Add(_thresholdCheckbox);
        Controls.Add(_thresholdValue);
        Controls.Add(cooldownLabel);
        Controls.Add(_cooldownValue);
        Controls.Add(_saveButton);

        AcceptButton = _saveButton;

        themeService?.ApplyTheme(this, "dark");
    }

    public AppSettings BuildUpdatedSettings(AppSettings existing)
    {
        existing.AutomationEnabled = _automationCheckbox.Checked;
        existing.StartupEnabled = _startupCheckbox.Checked;
        existing.TransitionsEnabled = _transitionsCheckbox.Checked;
        existing.SkipSmallBrightnessDifferences = _thresholdCheckbox.Checked;
        existing.SmallDifferenceThreshold = (byte)_thresholdValue.Value;
        existing.ManualChangeRestoreCooldownSeconds = (int)_cooldownValue.Value;
        existing.ThemeMode = "dark";
        return existing;
    }
}
