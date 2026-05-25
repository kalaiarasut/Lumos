using Lumos.UI.Controls;

namespace Lumos.Services.Interfaces;

public interface IThemeService
{
    ThemePalette GetPalette(string mode);
    void ApplyTheme(Control root, string mode);
}
