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

    public ObservableCollection<AppProfile> Profiles { get; } = new();
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

    private AppProfile? _selectedProfile;
    public AppProfile? SelectedProfile
    {
        get => _selectedProfile;
        set { SetProperty(ref _selectedProfile, value); }
    }

    public ICommand LoadSetCommand { get; }
    public ICommand SaveSetCommand { get; }
    public ICommand ApplySelectedProfileCommand { get; }
    public ICommand DeleteSelectedProfileCommand { get; }
    public ICommand SaveCommand { get; }

    public ProfilesViewModel(IProfileStore profileStore, IBrightnessProvider? brightnessProvider)
    {
        _profileStore = profileStore;
        _brightnessProvider = brightnessProvider;

        LoadSetCommand = new RelayCommand(LoadSet, () => SelectedProfileSet != null);
        SaveSetCommand = new AsyncRelayCommand(SaveSetAsync, () => !string.IsNullOrWhiteSpace(ProfileSetName));
        ApplySelectedProfileCommand = new AsyncRelayCommand(ApplySelectedProfileAsync, () => SelectedProfile != null && _brightnessProvider != null);
        DeleteSelectedProfileCommand = new RelayCommand(DeleteSelectedProfile, () => SelectedProfile != null);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public async Task LoadAsync()
    {
        var profiles = await _profileStore.LoadProfilesAsync();
        var sets = await _profileStore.LoadProfileSetsAsync();

        Profiles.Clear();
        foreach (var p in profiles)
        {
            Profiles.Add(CloneProfile(p));
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
            Profiles.Add(CloneProfile(p));
        }
    }

    private async Task SaveSetAsync()
    {
        var name = string.IsNullOrWhiteSpace(ProfileSetName) ? "default" : ProfileSetName.Trim();
        var newSet = new ProfileSet
        {
            Name = name,
            Profiles = Profiles.Select(CloneProfile).ToList()
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
        await _brightnessProvider.SetBrightnessAsync(SelectedProfile.Brightness);
    }

    private void DeleteSelectedProfile()
    {
        if (SelectedProfile != null)
        {
            Profiles.Remove(SelectedProfile);
        }
    }

    private async Task SaveAsync()
    {
        await _profileStore.SaveProfilesAsync(Profiles.Select(CloneProfile).ToList());
    }

    private static AppProfile CloneProfile(AppProfile profile) =>
        new()
        {
            ExeName = profile.ExeName,
            DisplayName = profile.DisplayName,
            Brightness = profile.Brightness,
            Excluded = profile.Excluded,
            LastUpdatedUtc = profile.LastUpdatedUtc,
        };
}
