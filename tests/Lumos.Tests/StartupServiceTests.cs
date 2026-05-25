using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class StartupServiceTests
{
    [Fact]
    public void StartupRegistrationUsesCurrentUserRunKey()
    {
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Run", StartupService.RunKeyPath);
    }
}
