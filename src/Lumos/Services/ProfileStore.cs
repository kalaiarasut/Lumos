using System.Text.Json;
using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class ProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _rootPath;

    public ProfileStore(string rootPath)
    {
        _rootPath = rootPath;
    }

    public Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default) =>
        LoadOrDefaultAsync(GetSettingsPath(), AppSettings.CreateDefault, cancellationToken);

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default) =>
        SaveAsync(GetSettingsPath(), settings, cancellationToken);

    public Task<List<AppProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default) =>
        LoadOrDefaultAsync(GetProfilesPath(), static () => [], cancellationToken);

    public Task SaveProfilesAsync(List<AppProfile> profiles, CancellationToken cancellationToken = default) =>
        SaveAsync(GetProfilesPath(), profiles, cancellationToken);

    public Task SaveProfileSetAsync(ProfileSet profileSet, CancellationToken cancellationToken = default) =>
        SaveAsync(Path.Combine(GetProfileSetsPath(), $"{profileSet.Name}.json"), profileSet, cancellationToken);

    public async Task<List<ProfileSet>> LoadProfileSetsAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(GetProfileSetsPath());
        var files = Directory.GetFiles(GetProfileSetsPath(), "*.json");
        var results = new List<ProfileSet>();

        foreach (var file in files)
        {
            var set = await LoadOrDefaultAsync(
                file,
                () => new ProfileSet
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Profiles = [],
                },
                cancellationToken);
            results.Add(set);
        }

        return results;
    }

    private async Task<T> LoadOrDefaultAsync<T>(string path, Func<T> factory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            return factory();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var loaded = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
            return loaded ?? factory();
        }
        catch (JsonException)
        {
            File.Move(path, $"{path}.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}", overwrite: true);
            return factory();
        }
    }

    private static async Task SaveAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private string GetSettingsPath() => Path.Combine(_rootPath, "settings.json");
    private string GetProfilesPath() => Path.Combine(_rootPath, "profiles.json");
    private string GetProfileSetsPath() => Path.Combine(_rootPath, "profile-sets");
}
