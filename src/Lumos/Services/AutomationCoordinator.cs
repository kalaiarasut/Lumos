using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class AutomationCoordinator : IDisposable
{
    private static readonly TimeSpan LearningDebounce = TimeSpan.FromSeconds(1);

    private readonly IActiveWindowService _activeWindowService;
    private readonly IBrightnessProvider _brightnessProvider;
    private readonly IProfileStore _profileStore;
    private readonly IClock _clock;
    private readonly ILoggingService _loggingService;

    private string? _lastForegroundExe;
    private byte? _pendingBrightness;
    private string? _pendingBrightnessExe;
    private DateTimeOffset? _pendingObservedAtUtc;
    private byte? _lastAutomationBrightness;
    private DateTimeOffset? _suppressUntilUtc;
    private DateTimeOffset? _manualRestoreCooldownUntilUtc;
    private CancellationTokenSource? _restoreCancellation;
    private bool _hasSeededStartupProfiles;

    public AutomationCoordinator(
        IActiveWindowService activeWindowService,
        IBrightnessProvider brightnessProvider,
        IProfileStore profileStore,
        IClock clock,
        ILoggingService loggingService,
        TransitionService? transitionService = null)
    {
        _activeWindowService = activeWindowService;
        _brightnessProvider = brightnessProvider;
        _profileStore = profileStore;
        _clock = clock;
        _loggingService = loggingService;
        TransitionService = transitionService;
    }

    public TransitionService? TransitionService { get; }

    public async Task SeedRunningAppsWithCurrentBrightnessAsync(CancellationToken cancellationToken = default)
    {
        if (_hasSeededStartupProfiles)
        {
            return;
        }

        _hasSeededStartupProfiles = true;

        var settings = await _profileStore.LoadSettingsAsync(cancellationToken);
        if (IsAutomationInactive(settings))
        {
            return;
        }

        var runningExecutables = _activeWindowService.GetRunningExecutableNames()
            .Where(exeName =>
                !string.IsNullOrWhiteSpace(exeName) &&
                !ActiveWindowService.IsIgnoredExecutable(exeName) &&
                !settings.ExcludedExecutables.Contains(exeName, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (runningExecutables.Count == 0)
        {
            return;
        }

        var currentBrightness = await _brightnessProvider.GetCurrentBrightnessAsync(cancellationToken);
        var profiles = await _profileStore.LoadProfilesAsync(cancellationToken);

        foreach (var exeName in runningExecutables)
        {
            var existing = profiles.FirstOrDefault(profile => string.Equals(profile.ExeName, exeName, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                profiles.Add(new AppProfile
                {
                    ExeName = exeName,
                    DisplayName = Path.GetFileNameWithoutExtension(exeName),
                    Brightness = currentBrightness,
                    Excluded = false,
                    LastUpdatedUtc = _clock.UtcNow,
                });
            }
            else
            {
                existing.Brightness = currentBrightness;
                existing.LastUpdatedUtc = _clock.UtcNow;
            }
        }

        await _profileStore.SaveProfilesAsync(profiles, cancellationToken);
        _loggingService.Info($"Seeded {runningExecutables.Count} running app profiles with startup brightness {currentBrightness}");
    }

    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _profileStore.LoadSettingsAsync(cancellationToken);
        if (IsAutomationInactive(settings))
        {
            return;
        }

        if (_manualRestoreCooldownUntilUtc is not null && _manualRestoreCooldownUntilUtc > _clock.UtcNow)
        {
            return;
        }

        var currentExe = _activeWindowService.GetForegroundExecutableName();
        if (string.IsNullOrWhiteSpace(currentExe) ||
            ActiveWindowService.IsIgnoredExecutable(currentExe) ||
            settings.ExcludedExecutables.Contains(currentExe, StringComparer.OrdinalIgnoreCase) ||
            string.Equals(currentExe, _lastForegroundExe, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastForegroundExe = currentExe;

        var profiles = await LoadProfilesForCurrentScheduleAsync(settings, cancellationToken);
        var profile = profiles.FirstOrDefault(x => string.Equals(x.ExeName, currentExe, StringComparison.OrdinalIgnoreCase));
        if (profile is null || profile.Excluded)
        {
            return;
        }

        var currentBrightness = await _brightnessProvider.GetCurrentBrightnessAsync(cancellationToken);
        var delta = Math.Abs(currentBrightness - profile.Brightness);
        if (settings.SkipSmallBrightnessDifferences && delta <= settings.SmallDifferenceThreshold)
        {
            return;
        }

        _restoreCancellation?.Cancel();
        _restoreCancellation?.Dispose();
        _restoreCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            if (settings.TransitionsEnabled && TransitionService is not null)
            {
                await TransitionService.ApplyAsync(
                    profile.Brightness,
                    settings.TransitionDurationMilliseconds,
                    _restoreCancellation.Token,
                    MarkAutomationBrightness);
            }
            else
            {
                await _brightnessProvider.SetBrightnessAsync(profile.Brightness, _restoreCancellation.Token);
                MarkAutomationBrightness(profile.Brightness);
            }

            _loggingService.Info($"Restored brightness {profile.Brightness} for {currentExe}");
        }
        catch (OperationCanceledException) when (_restoreCancellation.IsCancellationRequested)
        {
            _loggingService.Info($"Cancelled brightness restore for {currentExe}");
        }
        finally
        {
            _restoreCancellation?.Dispose();
            _restoreCancellation = null;
        }
    }

    public async Task RecordObservedBrightnessAsync(byte brightness, CancellationToken cancellationToken = default)
    {
        var settings = await _profileStore.LoadSettingsAsync(cancellationToken);
        if (IsAutomationInactive(settings))
        {
            return;
        }

        if (_lastAutomationBrightness == brightness &&
            _suppressUntilUtc is not null &&
            _suppressUntilUtc > _clock.UtcNow)
        {
            return;
        }

        if (_restoreCancellation is not null &&
            !_restoreCancellation.IsCancellationRequested &&
            _lastAutomationBrightness != brightness)
        {
            _restoreCancellation.Cancel();
        }

        var currentExe = _activeWindowService.GetForegroundExecutableName();
        if (string.IsNullOrWhiteSpace(currentExe) ||
            ActiveWindowService.IsIgnoredExecutable(currentExe) ||
            settings.ExcludedExecutables.Contains(currentExe, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        if (_pendingBrightness == brightness &&
            string.Equals(_pendingBrightnessExe, currentExe, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _manualRestoreCooldownUntilUtc = _clock.UtcNow.AddSeconds(settings.ManualChangeRestoreCooldownSeconds);
        _pendingBrightness = brightness;
        _pendingBrightnessExe = currentExe;
        _pendingObservedAtUtc = _clock.UtcNow;
    }

    public async Task FlushPendingLearningAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _profileStore.LoadSettingsAsync(cancellationToken);
        if (IsAutomationInactive(settings))
        {
            ClearPendingLearning();
            return;
        }

        if (_pendingBrightness is null || _pendingBrightnessExe is null || _pendingObservedAtUtc is null)
        {
            return;
        }

        if ((_clock.UtcNow - _pendingObservedAtUtc.Value) < LearningDebounce)
        {
            return;
        }

        var profiles = await _profileStore.LoadProfilesAsync(cancellationToken);
        var existing = profiles.FirstOrDefault(x => string.Equals(x.ExeName, _pendingBrightnessExe, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            profiles.Add(new AppProfile
            {
                ExeName = _pendingBrightnessExe,
                DisplayName = Path.GetFileNameWithoutExtension(_pendingBrightnessExe),
                Brightness = _pendingBrightness.Value,
                Excluded = false,
                LastUpdatedUtc = _clock.UtcNow,
            });
        }
        else
        {
            existing.Brightness = _pendingBrightness.Value;
            existing.LastUpdatedUtc = _clock.UtcNow;
        }

        await _profileStore.SaveProfilesAsync(profiles, cancellationToken);
        _loggingService.Info($"Learned brightness {_pendingBrightness.Value} for {_pendingBrightnessExe}");

        ClearPendingLearning();
    }

    private bool IsAutomationInactive(AppSettings settings) =>
        !settings.AutomationEnabled || (settings.PauseUntilUtc is not null && settings.PauseUntilUtc > _clock.UtcNow);

    private async Task<List<AppProfile>> LoadProfilesForCurrentScheduleAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (settings.ScheduledProfileEnabled &&
            IsWithinScheduledProfileWindow(settings) &&
            !string.IsNullOrWhiteSpace(settings.ScheduledProfileSetName))
        {
            var sets = await _profileStore.LoadProfileSetsAsync(cancellationToken);
            var set = sets.FirstOrDefault(x => string.Equals(x.Name, settings.ScheduledProfileSetName, StringComparison.OrdinalIgnoreCase));
            if (set is not null)
            {
                return set.Profiles;
            }
        }

        return await _profileStore.LoadProfilesAsync(cancellationToken);
    }

    private bool IsWithinScheduledProfileWindow(AppSettings settings)
    {
        if (!TimeOnly.TryParse(settings.ScheduledProfileStartTime, out var start) ||
            !TimeOnly.TryParse(settings.ScheduledProfileEndTime, out var end))
        {
            return false;
        }

        var now = TimeOnly.FromDateTime(_clock.UtcNow.ToLocalTime().DateTime);
        if (start == end)
        {
            return true;
        }

        return start < end
            ? now >= start && now < end
            : now >= start || now < end;
    }

    private void MarkAutomationBrightness(byte brightness)
    {
        _lastAutomationBrightness = brightness;
        _suppressUntilUtc = _clock.UtcNow.AddSeconds(2);
    }

    private void ClearPendingLearning()
    {
        _pendingBrightness = null;
        _pendingBrightnessExe = null;
        _pendingObservedAtUtc = null;
    }

    public void Dispose()
    {
        _restoreCancellation?.Cancel();
        _restoreCancellation?.Dispose();
        _restoreCancellation = null;
    }
}
