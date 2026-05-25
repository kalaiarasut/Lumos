using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class ActiveWindowServiceTests
{
    [Theory]
    [InlineData("SearchHost.exe")]
    [InlineData("ApplicationFrameHost.exe")]
    public void BuiltInIgnoredExecutablesAreIgnored(string exeName)
    {
        Assert.True(ActiveWindowService.IsIgnoredExecutable(exeName));
    }
}
