using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.Tests.Fakes;

public sealed class InMemoryProfileStore(AppSettings settings, List<AppProfile> profiles) : IProfileStore
{
    public AppSettings Settings { get; private set; } = settings;
    public List<AppProfile> Profiles { get; private set; } = profiles;
    public List<ProfileSet> ProfileSets { get; } = [];

    public Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Settings);

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.CompletedTask;
    }

    public Task<List<AppProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Profiles);

    public Task SaveProfilesAsync(List<AppProfile> profiles, CancellationToken cancellationToken = default)
    {
        Profiles = profiles;
        return Task.CompletedTask;
    }

    public Task SaveProfileSetAsync(ProfileSet profileSet, CancellationToken cancellationToken = default)
    {
        ProfileSets.Add(profileSet);
        return Task.CompletedTask;
    }

    public Task<List<ProfileSet>> LoadProfileSetsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ProfileSets);
}
