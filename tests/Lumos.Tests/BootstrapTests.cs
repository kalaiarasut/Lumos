using Xunit;

namespace Lumos.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void AppAssemblyIsReferenced()
    {
        Assert.Equal("Lumos", typeof(Lumos.Program).Assembly.GetName().Name);
    }
}
