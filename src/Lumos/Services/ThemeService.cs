using System.Drawing;
using Lumos.Services.Interfaces;
using Lumos.UI.Controls;

namespace Lumos.Services;

public sealed class ThemeService : IThemeService
{
    private static readonly ThemePalette DarkPalette = new(
        "dark",
        Color.FromArgb(32, 32, 36),
        Color.Gainsboro,
        Color.DeepSkyBlue);

    public static ThemePalette GetPalette(string mode) => DarkPalette;

    ThemePalette IThemeService.GetPalette(string mode) => GetPalette(mode);

    public void ApplyTheme(Control root, string mode)
    {
        ApplyRecursive(root, GetPalette(mode));
    }

    private static void ApplyRecursive(Control control, ThemePalette palette)
    {
        control.BackColor = palette.BackgroundColor;
        control.ForeColor = palette.ForegroundColor;

        foreach (Control child in control.Controls)
        {
            ApplyRecursive(child, palette);
        }
    }
}
