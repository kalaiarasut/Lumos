using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class LoggingService : ILoggingService
{
    private const long MaxLogBytes = 1024 * 1024;
    private readonly string _logPath;
    private readonly object _lock = new();

    public LoggingService(string logPath)
    {
        _logPath = logPath;
    }

    public void Info(string message) => Write("INFO", message, null);

    public void Warn(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.UtcNow:O} [{level}] {message}";
        if (exception is not null)
        {
            line += $" :: {exception}";
        }

        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            RotateIfNeeded();
            File.AppendAllLines(_logPath, [line]);
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logPath))
        {
            return;
        }

        var file = new FileInfo(_logPath);
        if (file.Length < MaxLogBytes)
        {
            return;
        }

        var archivePath = Path.Combine(
            file.DirectoryName!,
            $"{Path.GetFileNameWithoutExtension(_logPath)}.{DateTime.UtcNow:yyyyMMddHHmmss}.log");
        File.Move(_logPath, archivePath, overwrite: true);
    }
}
