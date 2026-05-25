using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class TransitionService
{
    private readonly IBrightnessProvider _brightnessProvider;

    public TransitionService(IBrightnessProvider brightnessProvider)
    {
        _brightnessProvider = brightnessProvider;
    }

    public async Task ApplyAsync(byte targetBrightness, int durationMilliseconds, CancellationToken cancellationToken)
    {
        var current = await _brightnessProvider.GetCurrentBrightnessAsync(cancellationToken);
        const int steps = 6;
        var delay = Math.Max(1, durationMilliseconds / steps);

        for (var step = 1; step <= steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = (byte)(current + ((targetBrightness - current) * step / steps));
            await _brightnessProvider.SetBrightnessAsync(next, cancellationToken);
            await Task.Delay(delay, cancellationToken);
        }
    }
}
