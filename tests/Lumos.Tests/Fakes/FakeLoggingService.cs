using Lumos.Services.Interfaces;

namespace Lumos.Tests.Fakes;

public sealed class FakeLoggingService : ILoggingService
{
    public List<string> Messages { get; } = [];

    public void Info(string message) => Messages.Add(message);

    public void Warn(string message) => Messages.Add(message);

    public void Error(string message, Exception? exception = null) =>
        Messages.Add(exception is null ? message : $"{message} :: {exception.Message}");
}
