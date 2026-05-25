namespace Lumos.Models;

public sealed class ProfileSet
{
    public required string Name { get; init; }
    public required List<AppProfile> Profiles { get; init; }
}
