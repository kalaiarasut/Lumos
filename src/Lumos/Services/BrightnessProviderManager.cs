using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class BrightnessProviderManager
{
    public BrightnessProviderManager(IEnumerable<IBrightnessProvider> providers)
    {
        Providers = providers.ToList().AsReadOnly();
        ActiveProvider = Providers.FirstOrDefault(provider => provider.IsSupported);
    }

    public IReadOnlyList<IBrightnessProvider> Providers { get; }

    public IBrightnessProvider? ActiveProvider { get; }

    public bool HasSupportedProvider => ActiveProvider is not null;
}
