using Lumos.Services.Interfaces;

namespace Lumos.Tests.Fakes;

public sealed class FakeBrightnessProvider(byte brightness) : IBrightnessProvider
{
    public string ProviderName => "fake";
    public bool IsSupported => true;
    public byte CurrentBrightness { get; private set; } = brightness;
    public byte LastSetBrightness { get; private set; } = brightness;

    public Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CurrentBrightness);

    public Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default)
    {
        CurrentBrightness = value;
        LastSetBrightness = value;
        return Task.CompletedTask;
    }
}
