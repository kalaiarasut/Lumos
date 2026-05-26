using Lumos.Services.Interfaces;

namespace Lumos.Tests.Fakes;

public sealed class FakeBrightnessProvider(byte brightness) : IBrightnessProvider
{
    public string ProviderName => "fake";
    public bool IsSupported => true;
    public byte CurrentBrightness { get; private set; } = brightness;
    public byte LastSetBrightness { get; private set; } = brightness;
    public int SetCount { get; private set; }

    public Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CurrentBrightness);

    public Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CurrentBrightness = value;
        LastSetBrightness = value;
        SetCount++;
        return Task.CompletedTask;
    }

    public void SimulateExternalBrightness(byte value)
    {
        CurrentBrightness = value;
    }
}
