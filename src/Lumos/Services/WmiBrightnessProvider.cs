using System.Management;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class WmiBrightnessProvider : IBrightnessProvider
{
    public string ProviderName => "wmi";

    public bool IsSupported
    {
        get
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\wmi");
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM WmiMonitorBrightness"));
                using var results = searcher.Get();
                return results.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scope = new ManagementScope(@"\\.\root\wmi");
        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM WmiMonitorBrightness"));
        using var results = searcher.Get();

        foreach (ManagementObject instance in results)
        {
            return Task.FromResult((byte)instance["CurrentBrightness"]);
        }

        throw new InvalidOperationException("No internal brightness target found.");
    }

    public Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var scope = new ManagementScope(@"\\.\root\wmi");
        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM WmiMonitorBrightnessMethods"));
        using var results = searcher.Get();

        foreach (ManagementObject instance in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            instance.InvokeMethod("WmiSetBrightness", [uint.MaxValue, value]);
        }

        return Task.CompletedTask;
    }
}
