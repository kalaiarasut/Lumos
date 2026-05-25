using Lumos.Models;
using Lumos.Services;
using Lumos.Tests.Fakes;
using Xunit;

namespace Lumos.Tests;

public sealed class AutomationCoordinatorTests
{
    [Fact]
    public async Task RestoresSavedBrightnessForKnownApp()
    {
        var brightness = new FakeBrightnessProvider(70);
        var profiles = new InMemoryProfileStore(
            AppSettings.CreateDefault(),
            [
                new AppProfile
                {
                    ExeName = "chrome.exe",
                    DisplayName = "Chrome",
                    Brightness = 30,
                    Excluded = false,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                }
            ]);
        var activeWindow = new FakeActiveWindowService("chrome.exe");
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, new FakeClock(), new FakeLoggingService());

        await coordinator.TickAsync();

        Assert.Equal((byte)30, brightness.LastSetBrightness);
    }

    [Fact]
    public async Task LearnsManualBrightnessAfterDebounce()
    {
        var brightness = new FakeBrightnessProvider(40);
        var profiles = new InMemoryProfileStore(AppSettings.CreateDefault(), []);
        var activeWindow = new FakeActiveWindowService("code.exe");
        var clock = new FakeClock();
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, clock, new FakeLoggingService());

        await coordinator.RecordObservedBrightnessAsync(55);
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        await coordinator.FlushPendingLearningAsync();

        Assert.Single(profiles.Profiles);
        Assert.Equal("code.exe", profiles.Profiles[0].ExeName);
        Assert.Equal((byte)55, profiles.Profiles[0].Brightness);
    }
}
