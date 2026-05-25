namespace Lumos.Services.Interfaces;

public interface IBrightnessProvider
{
    string ProviderName { get; }
    bool IsSupported { get; }
    Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default);
    Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default);
}
