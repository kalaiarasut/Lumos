using Lumos.Services.Interfaces;

namespace Lumos.Tests.Fakes;

public sealed class FakeActiveWindowService(string exeName) : IActiveWindowService
{
    public string? ForegroundExecutableName { get; set; } = exeName;
    public IReadOnlyCollection<string> RunningExecutableNames { get; set; } = [];

    public string? GetForegroundExecutableName() => ForegroundExecutableName;
    public IReadOnlyCollection<string> GetRunningExecutableNames() => RunningExecutableNames;
}
