namespace Lumos.Models;

public sealed class InstalledAppOption
{
    public required string DisplayName { get; init; }
    public required string ExeName { get; init; }
    public string? SourcePath { get; init; }

    public string DisplayText => $"{DisplayName} ({ExeName})";
}
