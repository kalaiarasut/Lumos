namespace Lumos.Services.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
