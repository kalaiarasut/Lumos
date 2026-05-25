using Lumos.Models;

namespace Lumos.Services.Interfaces;

public interface IProfileStore
{
    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);
    Task<List<AppProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default);
    Task SaveProfilesAsync(List<AppProfile> profiles, CancellationToken cancellationToken = default);
    Task SaveProfileSetAsync(ProfileSet profileSet, CancellationToken cancellationToken = default);
    Task<List<ProfileSet>> LoadProfileSetsAsync(CancellationToken cancellationToken = default);
}
