using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
