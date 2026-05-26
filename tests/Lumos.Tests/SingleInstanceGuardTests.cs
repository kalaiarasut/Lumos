using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void SecondGuardDoesNotOwnSameInstance()
    {
        var mutexName = $"Lumos.Tests.{Guid.NewGuid():N}";
        using var first = new SingleInstanceGuard(mutexName);
        using var second = new SingleInstanceGuard(mutexName);

        Assert.True(first.IsOwner);
        Assert.False(second.IsOwner);
    }
}
