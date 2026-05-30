using System.Threading.Tasks;
using System.Windows.Input;
using Lumos.Models;
using Lumos.Services;
using Lumos.Services.Interfaces;

namespace Lumos.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly IProfileStore _profileStore;
    private readonly StartupService _startupService;
    private AppSettings _settings = new();

    public SettingsViewModel(IProfileStore profileStore, StartupService startupService)
    {
        _profileStore = profileStore;
        _startupService = startupService;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public ICommand SaveCommand { get; }

    public bool AutomationEnabled
    {
        get => _settings.AutomationEnabled;
        set { _settings.AutomationEnabled = value; OnPropertyChanged(); }
    }

    public bool StartupEnabled
    {
        get => _settings.StartupEnabled;
        set { _settings.StartupEnabled = value; OnPropertyChanged(); }
    }

    public bool TransitionsEnabled
    {
        get => _settings.TransitionsEnabled;
        set { _settings.TransitionsEnabled = value; OnPropertyChanged(); }
    }

    public bool SkipSmallBrightnessDifferences
    {
        get => _settings.SkipSmallBrightnessDifferences;
        set { _settings.SkipSmallBrightnessDifferences = value; OnPropertyChanged(); }
    }

    public byte SmallDifferenceThreshold
    {
        get => _settings.SmallDifferenceThreshold;
        set { _settings.SmallDifferenceThreshold = value; OnPropertyChanged(); }
    }

    public int ManualChangeRestoreCooldownSeconds
    {
        get => _settings.ManualChangeRestoreCooldownSeconds;
        set { _settings.ManualChangeRestoreCooldownSeconds = value; OnPropertyChanged(); }
    }

    public string ThemeMode
    {
        get => _settings.ThemeMode;
        set 
        {
            if (_settings.ThemeMode != value)
            {
                _settings.ThemeMode = value; 
                OnPropertyChanged();
                Lumos.UI.WPF.ThemeManager.ApplyTheme(value);
            }
        }
    }

    public async Task LoadAsync()
    {
        _settings = await _profileStore.LoadSettingsAsync();
        OnPropertyChanged(nameof(AutomationEnabled));
        OnPropertyChanged(nameof(StartupEnabled));
        OnPropertyChanged(nameof(TransitionsEnabled));
        OnPropertyChanged(nameof(SkipSmallBrightnessDifferences));
        OnPropertyChanged(nameof(SmallDifferenceThreshold));
        OnPropertyChanged(nameof(ManualChangeRestoreCooldownSeconds));
        OnPropertyChanged(nameof(ThemeMode));
        
        if (System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                Lumos.UI.WPF.ThemeManager.ApplyTheme(_settings.ThemeMode);
            });
        }
    }

    private async Task SaveAsync()
    {
        await _profileStore.SaveSettingsAsync(_settings);
        _startupService.SetEnabled(System.Windows.Forms.Application.ExecutablePath, _settings.StartupEnabled);
    }
}
