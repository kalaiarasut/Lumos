using Lumos.Services.Interfaces;

namespace Lumos.UI;

public sealed class FirstRunForm : Form
{
    private readonly CheckBox _startupCheckbox;

    public FirstRunForm(IThemeService? themeService = null)
    {
        Text = "Lumos Setup";
        Width = 420;
        Height = 280;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var infoLabel = new Label
        {
            AutoSize = false,
            Width = 360,
            Height = 100,
            Left = 20,
            Top = 20,
            Text = "Lumos remembers brightness per app and stores settings locally.",
        };

        _startupCheckbox = new CheckBox
        {
            Left = 20,
            Top = 130,
            Width = 220,
            Text = "Start Lumos with Windows",
        };

        var continueButton = new Button
        {
            Left = 280,
            Top = 200,
            Width = 100,
            Height = 32,
            Text = "Continue",
            DialogResult = DialogResult.OK,
        };

        Controls.Add(infoLabel);
        Controls.Add(_startupCheckbox);
        Controls.Add(continueButton);

        AcceptButton = continueButton;

        themeService?.ApplyTheme(this, "dark");
    }

    public bool StartWithWindows => _startupCheckbox.Checked;
}
