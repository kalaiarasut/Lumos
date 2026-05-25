using Lumos.Models;
using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class ThemeServiceTests
{
    [Fact]
    public void DefaultSettingsUseDarkTheme()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal("dark", settings.ThemeMode);
        Assert.True(settings.TransitionsEnabled);
        Assert.False(settings.SkipSmallBrightnessDifferences);
    }

    [Fact]
    public void DarkThemeReturnsDarkPalette()
    {
        var palette = ThemeService.GetPalette("dark");

        Assert.Equal("dark", palette.Mode);
        Assert.NotEqual(palette.BackgroundColor, palette.ForegroundColor);
    }
}
