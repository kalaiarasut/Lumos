using Lumos.Services;
using Lumos.Tests.Fakes;
using Xunit;

namespace Lumos.Tests;

public sealed class TransitionServiceTests
{
    [Fact]
    public async Task AppliesTargetBrightnessAtEndOfTransition()
    {
        var provider = new FakeBrightnessProvider(20);
        var service = new TransitionService(provider);

        await service.ApplyAsync(60, 100, CancellationToken.None);

        Assert.Equal((byte)60, provider.CurrentBrightness);
    }
}
