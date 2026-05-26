using System.Threading.Tasks;
using System.Windows.Input;
using Lumos.Services;
using Lumos.Services.Interfaces;

namespace Lumos.ViewModels;

public class MainViewModel : ObservableObject
{
    public SettingsViewModel SettingsVM { get; }
    public ProfilesViewModel ProfilesVM { get; }

    private object _currentView;
    public object CurrentView
    {
        get => _currentView;
        set { SetProperty(ref _currentView, value); }
    }

    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToProfilesCommand { get; }

    public MainViewModel(IProfileStore profileStore, StartupService startupService, IBrightnessProvider? brightnessProvider)
    {
        SettingsVM = new SettingsViewModel(profileStore, startupService);
        ProfilesVM = new ProfilesViewModel(profileStore, brightnessProvider);

        NavigateToSettingsCommand = new RelayCommand(() => CurrentView = SettingsVM);
        NavigateToProfilesCommand = new RelayCommand(() => CurrentView = ProfilesVM);

        // Default view
        _currentView = SettingsVM;
    }

    public async Task InitializeAsync()
    {
        await SettingsVM.LoadAsync();
        await ProfilesVM.LoadAsync();
    }
}
