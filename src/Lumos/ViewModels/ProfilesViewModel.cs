using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.ViewModels;

public class ProfilesViewModel : ObservableObject
{
    private readonly IProfileStore _profileStore;
    private readonly IBrightnessProvider? _brightnessProvider;

    public ObservableCollection<AppProfileViewModel> Profiles { get; } = new();
    public ObservableCollection<ProfileSet> ProfileSets { get; } = new();

    private ProfileSet? _selectedProfileSet;
    public ProfileSet? SelectedProfileSet
    {
        get => _selectedProfileSet;
        set 
        { 
            if (SetProperty(ref _selectedProfileSet, value))
            {
                ProfileSetName = value?.Name ?? "";
            }
        }
    }

    private string _profileSetName = "";
    public string ProfileSetName
    {
        get => _profileSetName;
        set 
        { 
            SetProperty(ref _profileSetName, value);
        }
    }

    private AppProfileViewModel? _selectedProfile;
    public AppProfileViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set { SetProperty(ref _selectedProfile, value); }
    }

    public ICommand LoadSetCommand { get; }
    public ICommand SaveSetCommand { get; }
    public ICommand ApplySelectedProfileCommand { get; }
    public ICommand DeleteSelectedProfileCommand { get; }
    public ICommand AddProfileCommand { get; }
    public ICommand NewSetCommand { get; }
    public ICommand SaveCommand { get; }

    private string _newDisplayName = "";
    public string NewDisplayName
    {
        get => _newDisplayName;
        set => SetProperty(ref _newDisplayName, value);
    }

    private string _newExeName = "";
    public string NewExeName
    {
        get => _newExeName;
        set => SetProperty(ref _newExeName, value);
    }

    private int _newBrightness = 50;
    public int NewBrightness
    {
        get => _newBrightness;
        set => SetProperty(ref _newBrightness, Math.Clamp(value, 0, 100));
    }

    public ProfilesViewModel(IProfileStore profileStore, IBrightnessProvider? brightnessProvider)
    {
        _profileStore = profileStore;
        _brightnessProvider = brightnessProvider;

        LoadSetCommand = new RelayCommand(LoadSet, () => SelectedProfileSet != null);
        SaveSetCommand = new AsyncRelayCommand(SaveSetAsync, () => !string.IsNullOrWhiteSpace(ProfileSetName));
        ApplySelectedProfileCommand = new AsyncRelayCommand(ApplySelectedProfileAsync, () => SelectedProfile != null && _brightnessProvider != null);
        DeleteSelectedProfileCommand = new RelayCommand(DeleteSelectedProfile, () => SelectedProfile != null);
        AddProfileCommand = new RelayCommand(AddProfile);
        NewSetCommand = new RelayCommand(CreateNewSet);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public async Task LoadAsync()
    {
        var profiles = await _profileStore.LoadProfilesAsync();
        var sets = await _profileStore.LoadProfileSetsAsync();

        Profiles.Clear();
        foreach (var p in profiles)
        {
            Profiles.Add(new AppProfileViewModel(p));
        }

        ProfileSets.Clear();
        foreach (var s in sets)
        {
            ProfileSets.Add(s);
        }

        SelectedProfileSet = ProfileSets.FirstOrDefault();
    }

    private void LoadSet()
    {
        if (SelectedProfileSet == null) return;
        Profiles.Clear();
        foreach (var p in SelectedProfileSet.Profiles)
        {
            Profiles.Add(new AppProfileViewModel(p));
        }
    }

    private async Task SaveSetAsync()
    {
        var name = string.IsNullOrWhiteSpace(ProfileSetName) ? "default" : ProfileSetName.Trim();
        var newSet = new ProfileSet
        {
            Name = name,
            Profiles = Profiles.Select(p => p.ToProfile()).ToList()
        };
        await _profileStore.SaveProfileSetAsync(newSet);
        
        var existing = ProfileSets.FirstOrDefault(s => s.Name == name);
        if (existing == null)
        {
            ProfileSets.Add(newSet);
            SelectedProfileSet = newSet;
        }
        else
        {
            var index = ProfileSets.IndexOf(existing);
            ProfileSets[index] = newSet;
            SelectedProfileSet = newSet;
        }
    }

    private async Task ApplySelectedProfileAsync()
    {
        if (_brightnessProvider == null || SelectedProfile == null) return;
        await _brightnessProvider.SetBrightnessAsync((byte)SelectedProfile.Brightness);
    }

    private void DeleteSelectedProfile()
    {
        if (SelectedProfile != null)
        {
            Profiles.Remove(SelectedProfile);
        }
    }

    private void AddProfile()
    {
        if (string.IsNullOrWhiteSpace(NewExeName))
        {
            return;
        }

        var exeName = NewExeName.Trim();
        if (!exeName.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase))
        {
            exeName += ".exe";
        }

        var profile = new AppProfileViewModel(new AppProfile
        {
            ExeName = exeName,
            DisplayName = string.IsNullOrWhiteSpace(NewDisplayName) ? Path.GetFileNameWithoutExtension(exeName) : NewDisplayName.Trim(),
            Brightness = (byte)Math.Clamp(NewBrightness, 0, 100),
            Excluded = false,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
        });

        Profiles.Add(profile);
        SelectedProfile = profile;
        NewDisplayName = "";
        NewExeName = "";
        NewBrightness = 50;
    }

    private void CreateNewSet()
    {
        Profiles.Clear();
        SelectedProfile = null;
        SelectedProfileSet = null;
        ProfileSetName = $"set-{DateTimeOffset.Now:yyyyMMdd-HHmm}";
    }

    private async Task SaveAsync()
    {
        await _profileStore.SaveProfilesAsync(Profiles.Select(p => p.ToProfile()).ToList());
    }
}
