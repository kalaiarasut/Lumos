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

    [Fact]
    public async Task LearnsManualBrightnessImmediatelyAfterAutomaticRestore()
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
        var clock = new FakeClock();
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, clock, new FakeLoggingService());

        await coordinator.TickAsync();
        await coordinator.RecordObservedBrightnessAsync(55);
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        await coordinator.FlushPendingLearningAsync();

        Assert.Single(profiles.Profiles);
        Assert.Equal((byte)55, profiles.Profiles[0].Brightness);
    }

    [Fact]
    public async Task DoesNotLearnWhenAutomationIsDisabled()
    {
        var settings = AppSettings.CreateDefault();
        settings.AutomationEnabled = false;
        var profiles = new InMemoryProfileStore(settings, []);
        var activeWindow = new FakeActiveWindowService("code.exe");
        var clock = new FakeClock();
        var coordinator = new AutomationCoordinator(activeWindow, new FakeBrightnessProvider(40), profiles, clock, new FakeLoggingService());

        await coordinator.RecordObservedBrightnessAsync(55);
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        await coordinator.FlushPendingLearningAsync();

        Assert.Empty(profiles.Profiles);
    }

    [Fact]
    public async Task DoesNotLearnWhenAutomationIsPaused()
    {
        var clock = new FakeClock();
        var settings = AppSettings.CreateDefault();
        settings.PauseUntilUtc = clock.UtcNow.AddMinutes(30);
        var profiles = new InMemoryProfileStore(settings, []);
        var activeWindow = new FakeActiveWindowService("code.exe");
        var coordinator = new AutomationCoordinator(activeWindow, new FakeBrightnessProvider(40), profiles, clock, new FakeLoggingService());

        await coordinator.RecordObservedBrightnessAsync(55);
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        await coordinator.FlushPendingLearningAsync();

        Assert.Empty(profiles.Profiles);
    }

    [Fact]
    public async Task ManualBrightnessDuringTransitionCancelsOldRestore()
    {
        var settings = AppSettings.CreateDefault();
        settings.TransitionDurationMilliseconds = 1200;
        var brightness = new FakeBrightnessProvider(70);
        var profiles = new InMemoryProfileStore(
            settings,
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
        var clock = new FakeClock();
        var transition = new TransitionService(brightness);
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, clock, new FakeLoggingService(), transition);

        var restoreTask = coordinator.TickAsync();
        await WaitUntilAsync(() => brightness.SetCount > 0);
        brightness.SimulateExternalBrightness(55);

        await coordinator.RecordObservedBrightnessAsync(55);
        await restoreTask;

        Assert.Equal((byte)55, brightness.CurrentBrightness);
    }

    [Fact]
    public async Task ManualBrightnessStartsRestoreCooldownButStillLearns()
    {
        var settings = AppSettings.CreateDefault();
        settings.ManualChangeRestoreCooldownSeconds = 8;
        var brightness = new FakeBrightnessProvider(70);
        var profiles = new InMemoryProfileStore(
            settings,
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
        var clock = new FakeClock();
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, clock, new FakeLoggingService());

        await coordinator.RecordObservedBrightnessAsync(55);
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        await coordinator.FlushPendingLearningAsync();
        activeWindow.ForegroundExecutableName = "code.exe";
        await coordinator.TickAsync();
        activeWindow.ForegroundExecutableName = "chrome.exe";
        await coordinator.TickAsync();

        Assert.Equal((byte)70, brightness.CurrentBrightness);
        Assert.Equal((byte)55, profiles.Profiles.Single(x => x.ExeName == "chrome.exe").Brightness);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, timeout.Token);
        }
    }
}
