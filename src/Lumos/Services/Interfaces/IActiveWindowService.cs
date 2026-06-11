namespace Lumos.Services.Interfaces;

public interface IActiveWindowService
{
    string? GetForegroundExecutableName();
    IReadOnlyCollection<string> GetRunningExecutableNames();
}
