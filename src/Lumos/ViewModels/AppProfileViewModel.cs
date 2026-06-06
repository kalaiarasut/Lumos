using Lumos.Models;

namespace Lumos.ViewModels;

public sealed class AppProfileViewModel : ObservableObject
{
    private string _exeName;
    private string _displayName;
    private int _brightness;
    private bool _excluded;
    private DateTimeOffset _lastUpdatedUtc;

    public AppProfileViewModel(AppProfile profile)
    {
        _exeName = profile.ExeName;
        _displayName = profile.DisplayName;
        _brightness = profile.Brightness;
        _excluded = profile.Excluded;
        _lastUpdatedUtc = profile.LastUpdatedUtc;
    }

    public string ExeName
    {
        get => _exeName;
        set => SetProperty(ref _exeName, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public int Brightness
    {
        get => _brightness;
        set => SetProperty(ref _brightness, Math.Clamp(value, 0, 100));
    }

    public bool Excluded
    {
        get => _excluded;
        set => SetProperty(ref _excluded, value);
    }

    public DateTimeOffset LastUpdatedUtc
    {
        get => _lastUpdatedUtc;
        set => SetProperty(ref _lastUpdatedUtc, value);
    }

    public AppProfile ToProfile() =>
        new()
        {
            ExeName = string.IsNullOrWhiteSpace(ExeName) ? "app.exe" : ExeName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? Path.GetFileNameWithoutExtension(ExeName) : DisplayName.Trim(),
            Brightness = (byte)Math.Clamp(Brightness, 0, 100),
            Excluded = Excluded,
            LastUpdatedUtc = LastUpdatedUtc == default ? DateTimeOffset.UtcNow : LastUpdatedUtc,
        };
}
