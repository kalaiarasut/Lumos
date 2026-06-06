using Lumos.Models;
using Lumos.Services;
using Lumos.Tests.Fakes;
using Lumos.ViewModels;
using Xunit;

namespace Lumos.Tests;

public class ViewModelsTests
{
    [Fact]
    public async Task SettingsViewModel_LoadsSettings()
    {
        var store = new InMemoryProfileStore(AppSettings.CreateDefault(), []);
        var settings = AppSettings.CreateDefault();
        settings.AutomationEnabled = false;
        settings.ManualChangeRestoreCooldownSeconds = 42;
        await store.SaveSettingsAsync(settings);

        var startup = new StartupService();
        var vm = new SettingsViewModel(store, startup);
        
        await vm.LoadAsync();

        Assert.False(vm.AutomationEnabled);
        Assert.Equal(42, vm.ManualChangeRestoreCooldownSeconds);
    }

    [Fact]
    public async Task SettingsViewModel_SavesSettings()
    {
        var store = new InMemoryProfileStore(AppSettings.CreateDefault(), []);
        var startup = new StartupService();
        var vm = new SettingsViewModel(store, startup);
        
        await vm.LoadAsync();
        vm.AutomationEnabled = false;
        vm.SmallDifferenceThreshold = 99;
        vm.ThemeMode = "light";
        
        vm.SaveCommand.Execute(null);
        // Wait a little since command executes async void
        await Task.Delay(50);

        var saved = await store.LoadSettingsAsync();
        Assert.False(saved.AutomationEnabled);
        Assert.Equal(99, saved.SmallDifferenceThreshold);
        Assert.Equal("light", saved.ThemeMode);
    }

    [Fact]
    public async Task ProfilesViewModel_LoadsProfilesAndSets()
    {
        var store = new InMemoryProfileStore(AppSettings.CreateDefault(), []);
        await store.SaveProfilesAsync(new List<AppProfile> 
        { 
            new AppProfile { ExeName = "test.exe", DisplayName = "test", Brightness = 50 } 
        });

        await store.SaveProfileSetAsync(new ProfileSet 
        { 
            Name = "custom", 
            Profiles = new List<AppProfile> { new AppProfile { ExeName = "test2.exe", DisplayName = "test2", Brightness = 80 } }
        });

        var provider = new FakeBrightnessProvider(100);
        var vm = new ProfilesViewModel(store, provider, () => []);
        
        await vm.LoadAsync();

        Assert.Single(vm.Profiles);
        Assert.Equal("test.exe", vm.Profiles[0].ExeName);
        Assert.Single(vm.ProfileSets);
        Assert.Contains(vm.ProfileSets, s => s.Name == "custom");
    }

    [Fact]
    public void ProfilesViewModel_SelectInstalledApp_PrefillsManualProfileFields()
    {
        var store = new InMemoryProfileStore(AppSettings.CreateDefault(), []);
        var provider = new FakeBrightnessProvider(100);
        var vm = new ProfilesViewModel(store, provider, () => []);
        var app = new InstalledAppOption
        {
            DisplayName = "Visual Studio Code",
            ExeName = "Code.exe"
        };

        vm.SelectedInstalledApp = app;

        Assert.Equal("Visual Studio Code", vm.NewDisplayName);
        Assert.Equal("Code.exe", vm.NewExeName);
    }

    [Fact]
    public void AppProfileViewModel_BrightnessText_CommitsTypedNumber()
    {
        var vm = new AppProfileViewModel(new AppProfile
        {
            ExeName = "test.exe",
            DisplayName = "test",
            Brightness = 100
        });

        vm.BrightnessText = "40";

        Assert.Equal(40, vm.Brightness);
        Assert.Equal("40", vm.BrightnessText);
    }

    [Fact]
    public void AppProfileViewModel_BrightnessText_RevertsInvalidText()
    {
        var vm = new AppProfileViewModel(new AppProfile
        {
            ExeName = "test.exe",
            DisplayName = "test",
            Brightness = 35
        });

        vm.BrightnessText = "";

        Assert.Equal(35, vm.Brightness);
        Assert.Equal("35", vm.BrightnessText);
    }

    [Fact]
    public void MainViewModel_Navigation()
    {
        var store = new InMemoryProfileStore(AppSettings.CreateDefault(), []);
        var startup = new StartupService();
        var provider = new FakeBrightnessProvider(100);
        
        var vm = new MainViewModel(store, startup, provider);
        
        Assert.IsType<SettingsViewModel>(vm.CurrentView);
        
        vm.NavigateToProfilesCommand.Execute(null);
        Assert.IsType<ProfilesViewModel>(vm.CurrentView);
        
        vm.NavigateToSettingsCommand.Execute(null);
        Assert.IsType<SettingsViewModel>(vm.CurrentView);
    }
}
