namespace Lumos.Models;

public sealed class AppProfile
{
    public required string ExeName { get; init; }
    public required string DisplayName { get; set; }
    public byte Brightness { get; set; }
    public bool Excluded { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
}
