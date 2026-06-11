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
    public async Task StartupSeedsRunningAppsWithCurrentBrightness()
    {
        var brightness = new FakeBrightnessProvider(42);
        var profiles = new InMemoryProfileStore(
            AppSettings.CreateDefault(),
            [
                new AppProfile
                {
                    ExeName = "chrome.exe",
                    DisplayName = "Chrome",
                    Brightness = 10,
                    Excluded = false,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                }
            ]);
        var activeWindow = new FakeActiveWindowService("chrome.exe")
        {
            RunningExecutableNames = ["chrome.exe", "code.exe", "Lumos.exe"],
        };
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, new FakeClock(), new FakeLoggingService());

        await coordinator.SeedRunningAppsWithCurrentBrightnessAsync();

        Assert.Equal((byte)42, profiles.Profiles.Single(profile => profile.ExeName == "chrome.exe").Brightness);
        Assert.Equal((byte)42, profiles.Profiles.Single(profile => profile.ExeName == "code.exe").Brightness);
        Assert.DoesNotContain(profiles.Profiles, profile => profile.ExeName == "Lumos.exe");
    }

    [Fact]
    public async Task ManualBrightnessAfterStartupUpdatesOnlyForegroundApp()
    {
        var brightness = new FakeBrightnessProvider(40);
        var profiles = new InMemoryProfileStore(AppSettings.CreateDefault(), []);
        var activeWindow = new FakeActiveWindowService("chrome.exe")
        {
            RunningExecutableNames = ["chrome.exe", "code.exe"],
        };
        var clock = new FakeClock();
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, clock, new FakeLoggingService());

        await coordinator.SeedRunningAppsWithCurrentBrightnessAsync();
        await coordinator.RecordObservedBrightnessAsync(55);
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        await coordinator.FlushPendingLearningAsync();

        Assert.Equal((byte)55, profiles.Profiles.Single(profile => profile.ExeName == "chrome.exe").Brightness);
        Assert.Equal((byte)40, profiles.Profiles.Single(profile => profile.ExeName == "code.exe").Brightness);
    }

    [Fact]
    public async Task DoesNotRestoreOrLearnForLumosWindow()
    {
        var brightness = new FakeBrightnessProvider(40);
        var profiles = new InMemoryProfileStore(
            AppSettings.CreateDefault(),
            [
                new AppProfile
                {
                    ExeName = "Lumos.exe",
                    DisplayName = "Lumos",
                    Brightness = 100,
                    Excluded = false,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                }
            ]);
        var activeWindow = new FakeActiveWindowService("Lumos.exe");
        var clock = new FakeClock();
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, clock, new FakeLoggingService());

        await coordinator.TickAsync();
        await coordinator.RecordObservedBrightnessAsync(90);
        clock.UtcNow = clock.UtcNow.AddSeconds(2);
        await coordinator.FlushPendingLearningAsync();

        Assert.Equal((byte)40, brightness.CurrentBrightness);
        Assert.Equal((byte)100, profiles.Profiles.Single().Brightness);
    }

    [Fact]
    public async Task UsesScheduledProfileSetInsideTimeWindow()
    {
        var settings = AppSettings.CreateDefault();
        settings.ScheduledProfileEnabled = true;
        settings.ScheduledProfileSetName = "work";
        settings.ScheduledProfileStartTime = "06:00";
        settings.ScheduledProfileEndTime = "19:00";

        var store = new InMemoryProfileStore(
            settings,
            [
                new AppProfile
                {
                    ExeName = "chrome.exe",
                    DisplayName = "Chrome",
                    Brightness = 70,
                    Excluded = false,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                }
            ]);
        await store.SaveProfileSetAsync(new ProfileSet
        {
            Name = "work",
            Profiles =
            [
                new AppProfile
                {
                    ExeName = "chrome.exe",
                    DisplayName = "Chrome",
                    Brightness = 25,
                    Excluded = false,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                }
            ],
        });

        var brightness = new FakeBrightnessProvider(80);
        var clock = new FakeClock { UtcNow = ToUtcLocalTime(2026, 1, 1, 9, 0) };
        var coordinator = new AutomationCoordinator(new FakeActiveWindowService("chrome.exe"), brightness, store, clock, new FakeLoggingService());

        await coordinator.TickAsync();

        Assert.Equal((byte)25, brightness.CurrentBrightness);
    }

    [Fact]
    public async Task UsesNormalProfilesOutsideScheduledTimeWindow()
    {
        var settings = AppSettings.CreateDefault();
        settings.ScheduledProfileEnabled = true;
        settings.ScheduledProfileSetName = "work";
        settings.ScheduledProfileStartTime = "06:00";
        settings.ScheduledProfileEndTime = "19:00";

        var store = new InMemoryProfileStore(
            settings,
            [
                new AppProfile
                {
                    ExeName = "chrome.exe",
                    DisplayName = "Chrome",
                    Brightness = 70,
                    Excluded = false,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                }
            ]);
        await store.SaveProfileSetAsync(new ProfileSet
        {
            Name = "work",
            Profiles =
            [
                new AppProfile
                {
                    ExeName = "chrome.exe",
                    DisplayName = "Chrome",
                    Brightness = 25,
                    Excluded = false,
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                }
            ],
        });

        var brightness = new FakeBrightnessProvider(80);
        var clock = new FakeClock { UtcNow = ToUtcLocalTime(2026, 1, 1, 21, 0) };
        var coordinator = new AutomationCoordinator(new FakeActiveWindowService("chrome.exe"), brightness, store, clock, new FakeLoggingService());

        await coordinator.TickAsync();

        Assert.Equal((byte)70, brightness.CurrentBrightness);
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

    private static DateTimeOffset ToUtcLocalTime(int year, int month, int day, int hour, int minute)
    {
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }
}
