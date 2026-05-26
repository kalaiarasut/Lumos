namespace Lumos.Models;

public sealed class AppSettings
{
    public bool AutomationEnabled { get; set; }
    public DateTimeOffset? PauseUntilUtc { get; set; }
    public bool StartupEnabled { get; set; }
    public bool TransitionsEnabled { get; set; }
    public int TransitionDurationMilliseconds { get; set; }
    public int ManualChangeRestoreCooldownSeconds { get; set; }
    public bool SkipSmallBrightnessDifferences { get; set; }
    public byte SmallDifferenceThreshold { get; set; }
    public bool VerboseLoggingEnabled { get; set; }
    public bool FirstRunCompleted { get; set; }
    public string ThemeMode { get; set; } = "dark";
    public List<string> ExcludedExecutables { get; set; } = [];
    public string ActiveProfileSetName { get; set; } = "default";

    public static AppSettings CreateDefault() =>
        new()
        {
            AutomationEnabled = true,
            StartupEnabled = true,
            TransitionsEnabled = true,
            TransitionDurationMilliseconds = 350,
            ManualChangeRestoreCooldownSeconds = 8,
            SkipSmallBrightnessDifferences = false,
            SmallDifferenceThreshold = 2,
            VerboseLoggingEnabled = false,
            FirstRunCompleted = false,
            ThemeMode = "dark",
        };
}
