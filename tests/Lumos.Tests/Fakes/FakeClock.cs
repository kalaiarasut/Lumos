using Lumos.Services.Interfaces;

namespace Lumos.Tests.Fakes;

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
