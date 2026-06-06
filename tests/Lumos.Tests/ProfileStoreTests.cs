using Lumos.Models;
using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class ProfileStoreTests
{
    [Fact]
    public async Task SavesAndLoadsProfilesAndSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new ProfileStore(root);

        var settings = AppSettings.CreateDefault();
        var profiles = new List<AppProfile>
        {
            new()
            {
                ExeName = "chrome.exe",
                DisplayName = "Chrome",
                Brightness = 30,
                Excluded = false,
                LastUpdatedUtc = DateTimeOffset.UtcNow,
            }
        };

        await store.SaveSettingsAsync(settings);
        await store.SaveProfilesAsync(profiles);

        var loadedSettings = await store.LoadSettingsAsync();
        var loadedProfiles = await store.LoadProfilesAsync();

        Assert.Equal("light", loadedSettings.ThemeMode);
        Assert.Single(loadedProfiles);
        Assert.Equal("chrome.exe", loadedProfiles[0].ExeName);
    }

    [Fact]
    public async Task CorruptSettingsResetToDefaults()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "settings.json"), "{not-json");

        var store = new ProfileStore(root);
        var settings = await store.LoadSettingsAsync();

        Assert.Equal("light", settings.ThemeMode);
        Assert.Single(Directory.GetFiles(root, "settings.json.corrupt.*"));
    }
}
