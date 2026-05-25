using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class AutomationCoordinator
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
    private DateTimeOffset? _suppressUntilUtc;

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

    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _profileStore.LoadSettingsAsync(cancellationToken);
        if (!settings.AutomationEnabled || (settings.PauseUntilUtc is not null && settings.PauseUntilUtc > _clock.UtcNow))
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

        var profiles = await _profileStore.LoadProfilesAsync(cancellationToken);
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

        if (settings.TransitionsEnabled && TransitionService is not null)
        {
            await TransitionService.ApplyAsync(profile.Brightness, settings.TransitionDurationMilliseconds, cancellationToken);
        }
        else
        {
            await _brightnessProvider.SetBrightnessAsync(profile.Brightness, cancellationToken);
        }

        _suppressUntilUtc = _clock.UtcNow.AddSeconds(2);
        _loggingService.Info($"Restored brightness {profile.Brightness} for {currentExe}");
    }

    public Task RecordObservedBrightnessAsync(byte brightness)
    {
        if (_suppressUntilUtc is not null && _suppressUntilUtc > _clock.UtcNow)
        {
            return Task.CompletedTask;
        }

        var currentExe = _activeWindowService.GetForegroundExecutableName();
        if (string.IsNullOrWhiteSpace(currentExe) || ActiveWindowService.IsIgnoredExecutable(currentExe))
        {
            return Task.CompletedTask;
        }

        if (_pendingBrightness == brightness &&
            string.Equals(_pendingBrightnessExe, currentExe, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        _pendingBrightness = brightness;
        _pendingBrightnessExe = currentExe;
        _pendingObservedAtUtc = _clock.UtcNow;
        return Task.CompletedTask;
    }

    public async Task FlushPendingLearningAsync(CancellationToken cancellationToken = default)
    {
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

        _pendingBrightness = null;
        _pendingBrightnessExe = null;
        _pendingObservedAtUtc = null;
    }
}
