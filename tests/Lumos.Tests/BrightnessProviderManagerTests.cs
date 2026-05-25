using Lumos.Services;
using Lumos.Services.Interfaces;
using Xunit;

namespace Lumos.Tests;

public sealed class BrightnessProviderManagerTests
{
    [Fact]
    public void ChoosesFirstSupportedProvider()
    {
        var unsupported = new StubBrightnessProvider("unsupported", false, 20);
        var supported = new StubBrightnessProvider("wmi", true, 30);

        var manager = new BrightnessProviderManager([unsupported, supported]);

        Assert.NotNull(manager.ActiveProvider);
        Assert.Equal("wmi", manager.ActiveProvider!.ProviderName);
    }

    private sealed class StubBrightnessProvider(string name, bool isSupported, byte brightness) : IBrightnessProvider
    {
        public string ProviderName => name;
        public bool IsSupported => isSupported;

        public Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(brightness);

        public Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
