# Lumos Brightness Memory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows tray utility that learns and restores internal-display brightness per application, with provider-based brightness access, minimal dark-themed forms, and a polished tray workflow.

**Architecture:** Create a .NET WinForms app with a provider abstraction around brightness access, a coordinator that owns app-switch and learning behavior, JSON-backed settings/profile persistence, and minimal first-run/settings/profile forms. Keep UI work intentionally basic except for the tray flow; implement only the dark theme now, but keep theme-related naming and plumbing ready for future light-theme support.

**Tech Stack:** .NET 8, C#, WinForms, WMI, xUnit, System.Text.Json, Windows native APIs via P/Invoke

---

## File Structure

- `Lumos.sln`
- `src/Lumos/Lumos.csproj`
- `src/Lumos/Program.cs`
- `src/Lumos/Models/AppProfile.cs`
- `src/Lumos/Models/AppSettings.cs`
- `src/Lumos/Models/ProfileSet.cs`
- `src/Lumos/Services/Interfaces/IActiveWindowService.cs`
- `src/Lumos/Services/Interfaces/IBrightnessProvider.cs`
- `src/Lumos/Services/Interfaces/IProfileStore.cs`
- `src/Lumos/Services/Interfaces/IClock.cs`
- `src/Lumos/Services/Interfaces/ILoggingService.cs`
- `src/Lumos/Services/Interfaces/IThemeService.cs`
- `src/Lumos/Services/ActiveWindowService.cs`
- `src/Lumos/Services/BrightnessProviderManager.cs`
- `src/Lumos/Services/WmiBrightnessProvider.cs`
- `src/Lumos/Services/ProfileStore.cs`
- `src/Lumos/Services/AutomationCoordinator.cs`
- `src/Lumos/Services/TransitionService.cs`
- `src/Lumos/Services/StartupService.cs`
- `src/Lumos/Services/LoggingService.cs`
- `src/Lumos/Services/SystemClock.cs`
- `src/Lumos/Services/ThemeService.cs`
- `src/Lumos/UI/TrayApplicationContext.cs`
- `src/Lumos/UI/FirstRunForm.cs`
- `src/Lumos/UI/SettingsForm.cs`
- `src/Lumos/UI/ProfilesForm.cs`
- `src/Lumos/UI/Controls/ThemePalette.cs`
- `tests/Lumos.Tests/Lumos.Tests.csproj`
- `tests/Lumos.Tests/Fakes/FakeActiveWindowService.cs`
- `tests/Lumos.Tests/Fakes/FakeBrightnessProvider.cs`
- `tests/Lumos.Tests/Fakes/FakeClock.cs`
- `tests/Lumos.Tests/Fakes/FakeLoggingService.cs`
- `tests/Lumos.Tests/Fakes/InMemoryProfileStore.cs`
- `tests/Lumos.Tests/AutomationCoordinatorTests.cs`
- `tests/Lumos.Tests/ProfileStoreTests.cs`
- `tests/Lumos.Tests/ThemeServiceTests.cs`
- `tests/Lumos.Tests/BrightnessProviderManagerTests.cs`

### Task 1: Create Solution Skeleton

**Files:**
- Create: `Lumos.sln`
- Create: `src/Lumos/Lumos.csproj`
- Create: `src/Lumos/Program.cs`
- Create: `tests/Lumos.Tests/Lumos.Tests.csproj`

- [ ] **Step 1: Write the failing project bootstrap check**

Create `tests/Lumos.Tests/BootstrapTests.cs`:

```csharp
using Xunit;

namespace Lumos.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void TestAssemblyLoads()
    {
        Assert.True(typeof(BootstrapTests).Assembly.GetName().Name == "Lumos.Tests");
    }
}
```

- [ ] **Step 2: Run test to verify the test project does not exist yet**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj`
Expected: FAIL with project or solution file not found

- [ ] **Step 3: Create the solution and projects**

Create `src/Lumos/Lumos.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Windows.Compatibility" Version="8.0.5" />
  </ItemGroup>
</Project>
```

Create `tests/Lumos.Tests/Lumos.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Lumos\Lumos.csproj" />
  </ItemGroup>
</Project>
```

Create `src/Lumos/Program.cs`:

```csharp
namespace Lumos;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run();
    }
}
```

- [ ] **Step 4: Create the solution and add both projects**

Run:

```powershell
dotnet new sln --name Lumos
dotnet sln .\Lumos.sln add .\src\Lumos\Lumos.csproj
dotnet sln .\Lumos.sln add .\tests\Lumos.Tests\Lumos.Tests.csproj
```

Expected: both projects added to solution successfully

- [ ] **Step 5: Run tests to verify the skeleton passes**

Run: `dotnet test .\Lumos.sln`
Expected: PASS with `BootstrapTests.TestAssemblyLoads`

- [ ] **Step 6: Commit**

```bash
git add Lumos.sln src/Lumos tests/Lumos.Tests
git commit -m "chore: scaffold Lumos solution"
```

### Task 2: Define Core Models and Theme Settings

**Files:**
- Create: `src/Lumos/Models/AppProfile.cs`
- Create: `src/Lumos/Models/AppSettings.cs`
- Create: `src/Lumos/Models/ProfileSet.cs`
- Create: `tests/Lumos.Tests/ThemeServiceTests.cs`

- [ ] **Step 1: Write the failing settings default test**

Create `tests/Lumos.Tests/ThemeServiceTests.cs`:

```csharp
using Lumos.Models;
using Xunit;

namespace Lumos.Tests;

public sealed class ThemeServiceTests
{
    [Fact]
    public void DefaultSettingsUseDarkTheme()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal("dark", settings.ThemeMode);
        Assert.True(settings.TransitionsEnabled);
        Assert.False(settings.SkipSmallBrightnessDifferences);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter DefaultSettingsUseDarkTheme`
Expected: FAIL with `AppSettings` or `CreateDefault` missing

- [ ] **Step 3: Implement the minimal models**

Create `src/Lumos/Models/AppProfile.cs`:

```csharp
namespace Lumos.Models;

public sealed class AppProfile
{
    public required string ExeName { get; init; }
    public required string DisplayName { get; set; }
    public byte Brightness { get; set; }
    public bool Excluded { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; }
}
```

Create `src/Lumos/Models/AppSettings.cs`:

```csharp
namespace Lumos.Models;

public sealed class AppSettings
{
    public bool AutomationEnabled { get; set; }
    public DateTimeOffset? PauseUntilUtc { get; set; }
    public bool StartupEnabled { get; set; }
    public bool TransitionsEnabled { get; set; }
    public int TransitionDurationMilliseconds { get; set; }
    public bool SkipSmallBrightnessDifferences { get; set; }
    public byte SmallDifferenceThreshold { get; set; }
    public bool VerboseLoggingEnabled { get; set; }
    public bool FirstRunCompleted { get; set; }
    public string ThemeMode { get; set; } = "dark";
    public List<string> ExcludedExecutables { get; set; } = [];
    public string ActiveProfileSetName { get; set; } = "default";

    public static AppSettings CreateDefault() =>
        new()
        {
            AutomationEnabled = true,
            StartupEnabled = true,
            TransitionsEnabled = true,
            TransitionDurationMilliseconds = 350,
            SkipSmallBrightnessDifferences = false,
            SmallDifferenceThreshold = 2,
            VerboseLoggingEnabled = false,
            FirstRunCompleted = false,
            ThemeMode = "dark",
        };
}
```

Create `src/Lumos/Models/ProfileSet.cs`:

```csharp
namespace Lumos.Models;

public sealed class ProfileSet
{
    public required string Name { get; init; }
    public required List<AppProfile> Profiles { get; init; }
}
```

- [ ] **Step 4: Run tests to verify the models pass**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter DefaultSettingsUseDarkTheme`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Lumos/Models tests/Lumos.Tests/ThemeServiceTests.cs
git commit -m "feat: add core settings and profile models"
```

### Task 3: Build Persistence and File Recovery

**Files:**
- Create: `src/Lumos/Services/Interfaces/IProfileStore.cs`
- Create: `src/Lumos/Services/ProfileStore.cs`
- Create: `tests/Lumos.Tests/ProfileStoreTests.cs`

- [ ] **Step 1: Write the failing profile round-trip test**

Create `tests/Lumos.Tests/ProfileStoreTests.cs`:

```csharp
using Lumos.Models;
using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class ProfileStoreTests
{
    [Fact]
    public async Task SavesAndLoadsProfilesAndSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var store = new ProfileStore(root);

        var settings = AppSettings.CreateDefault();
        var profiles = new List<AppProfile>
        {
            new()
            {
                ExeName = "chrome.exe",
                DisplayName = "Chrome",
                Brightness = 30,
                Excluded = false,
                LastUpdatedUtc = DateTimeOffset.UtcNow
            }
        };

        await store.SaveSettingsAsync(settings);
        await store.SaveProfilesAsync(profiles);

        var loadedSettings = await store.LoadSettingsAsync();
        var loadedProfiles = await store.LoadProfilesAsync();

        Assert.Equal("dark", loadedSettings.ThemeMode);
        Assert.Single(loadedProfiles);
        Assert.Equal("chrome.exe", loadedProfiles[0].ExeName);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter SavesAndLoadsProfilesAndSettings`
Expected: FAIL with `ProfileStore` missing

- [ ] **Step 3: Implement the profile-store interface**

Create `src/Lumos/Services/Interfaces/IProfileStore.cs`:

```csharp
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
```

- [ ] **Step 4: Implement the minimal store with JSON and corrupt-file backup**

Create `src/Lumos/Services/ProfileStore.cs`:

```csharp
using System.Text.Json;
using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class ProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _rootPath;

    public ProfileStore(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        var path = GetSettingsPath();
        return await LoadOrDefaultAsync(path, AppSettings.CreateDefault, cancellationToken);
    }

    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default) =>
        SaveAsync(GetSettingsPath(), settings, cancellationToken);

    public async Task<List<AppProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default)
    {
        var path = GetProfilesPath();
        return await LoadOrDefaultAsync(path, () => [], cancellationToken);
    }

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
            results.Add(await LoadOrDefaultAsync(file, () => new ProfileSet { Name = Path.GetFileNameWithoutExtension(file), Profiles = [] }, cancellationToken));
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

    private async Task SaveAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private string GetSettingsPath() => Path.Combine(_rootPath, "settings.json");
    private string GetProfilesPath() => Path.Combine(_rootPath, "profiles.json");
    private string GetProfileSetsPath() => Path.Combine(_rootPath, "profile-sets");
}
```

- [ ] **Step 5: Run tests to verify the store passes**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter SavesAndLoadsProfilesAndSettings`
Expected: PASS

- [ ] **Step 6: Add a corrupt-settings recovery test**

Append to `tests/Lumos.Tests/ProfileStoreTests.cs`:

```csharp
[Fact]
public async Task CorruptSettingsResetToDefaults()
{
    var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    await File.WriteAllTextAsync(Path.Combine(root, "settings.json"), "{not-json");

    var store = new ProfileStore(root);
    var settings = await store.LoadSettingsAsync();

    Assert.Equal("dark", settings.ThemeMode);
    Assert.Single(Directory.GetFiles(root, "settings.json.corrupt.*"));
}
```

- [ ] **Step 7: Run tests to verify recovery passes**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter ProfileStoreTests`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/Lumos/Services/ProfileStore.cs src/Lumos/Services/Interfaces/IProfileStore.cs tests/Lumos.Tests/ProfileStoreTests.cs
git commit -m "feat: add profile persistence and recovery"
```

### Task 4: Add Brightness Provider Abstraction and WMI Backend

**Files:**
- Create: `src/Lumos/Services/Interfaces/IBrightnessProvider.cs`
- Create: `src/Lumos/Services/BrightnessProviderManager.cs`
- Create: `src/Lumos/Services/WmiBrightnessProvider.cs`
- Create: `tests/Lumos.Tests/BrightnessProviderManagerTests.cs`

- [ ] **Step 1: Write the failing provider-selection test**

Create `tests/Lumos.Tests/BrightnessProviderManagerTests.cs`:

```csharp
using Lumos.Services;
using Lumos.Services.Interfaces;
using Xunit;

namespace Lumos.Tests;

public sealed class BrightnessProviderManagerTests
{
    [Fact]
    public void ChoosesFirstSupportedProvider()
    {
        var unsupported = new StubBrightnessProvider("unsupported", false, 20);
        var supported = new StubBrightnessProvider("wmi", true, 30);

        var manager = new BrightnessProviderManager([unsupported, supported]);

        Assert.Equal("wmi", manager.ActiveProvider.ProviderName);
    }

    private sealed class StubBrightnessProvider(string name, bool isSupported, byte brightness) : IBrightnessProvider
    {
        public string ProviderName => name;
        public bool IsSupported => isSupported;
        public Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default) => Task.FromResult(brightness);
        public Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter ChoosesFirstSupportedProvider`
Expected: FAIL with `IBrightnessProvider` or `BrightnessProviderManager` missing

- [ ] **Step 3: Implement the abstraction and manager**

Create `src/Lumos/Services/Interfaces/IBrightnessProvider.cs`:

```csharp
namespace Lumos.Services.Interfaces;

public interface IBrightnessProvider
{
    string ProviderName { get; }
    bool IsSupported { get; }
    Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default);
    Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default);
}
```

Create `src/Lumos/Services/BrightnessProviderManager.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class BrightnessProviderManager
{
    public BrightnessProviderManager(IEnumerable<IBrightnessProvider> providers)
    {
        ActiveProvider = providers.FirstOrDefault(provider => provider.IsSupported)
            ?? throw new InvalidOperationException("No supported brightness provider found.");
    }

    public IBrightnessProvider ActiveProvider { get; }
}
```

- [ ] **Step 4: Implement the WMI provider**

Create `src/Lumos/Services/WmiBrightnessProvider.cs`:

```csharp
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
                using var scope = new ManagementScope(@"\\.\root\wmi");
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM WmiMonitorBrightness"));
                return searcher.Get().Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default)
    {
        using var scope = new ManagementScope(@"\\.\root\wmi");
        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM WmiMonitorBrightness"));
        foreach (ManagementObject instance in searcher.Get())
        {
            return Task.FromResult((byte)instance["CurrentBrightness"]);
        }

        throw new InvalidOperationException("No internal brightness target found.");
    }

    public Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default)
    {
        using var scope = new ManagementScope(@"\\.\root\wmi");
        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM WmiMonitorBrightnessMethods"));
        foreach (ManagementObject instance in searcher.Get())
        {
            instance.InvokeMethod("WmiSetBrightness", [uint.MaxValue, value]);
        }

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5: Run tests to verify the manager passes**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter ChoosesFirstSupportedProvider`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/Lumos/Services/Interfaces/IBrightnessProvider.cs src/Lumos/Services/BrightnessProviderManager.cs src/Lumos/Services/WmiBrightnessProvider.cs tests/Lumos.Tests/BrightnessProviderManagerTests.cs
git commit -m "feat: add brightness provider abstraction"
```

### Task 5: Implement Active Window Detection and Logging Interfaces

**Files:**
- Create: `src/Lumos/Services/Interfaces/IActiveWindowService.cs`
- Create: `src/Lumos/Services/Interfaces/ILoggingService.cs`
- Create: `src/Lumos/Services/ActiveWindowService.cs`
- Create: `src/Lumos/Services/LoggingService.cs`
- Create: `src/Lumos/Services/SystemClock.cs`
- Create: `src/Lumos/Services/Interfaces/IClock.cs`

- [ ] **Step 1: Write the failing ignored-process test**

Create `tests/Lumos.Tests/ActiveWindowServiceTests.cs`:

```csharp
using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class ActiveWindowServiceTests
{
    [Theory]
    [InlineData("SearchHost.exe")]
    [InlineData("ApplicationFrameHost.exe")]
    public void BuiltInIgnoredExecutablesAreIgnored(string exeName)
    {
        Assert.True(ActiveWindowService.IsIgnoredExecutable(exeName));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter BuiltInIgnoredExecutablesAreIgnored`
Expected: FAIL with `ActiveWindowService` missing

- [ ] **Step 3: Implement interfaces and helpers**

Create `src/Lumos/Services/Interfaces/IActiveWindowService.cs`:

```csharp
namespace Lumos.Services.Interfaces;

public interface IActiveWindowService
{
    string? GetForegroundExecutableName();
}
```

Create `src/Lumos/Services/Interfaces/ILoggingService.cs`:

```csharp
namespace Lumos.Services.Interfaces;

public interface ILoggingService
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}
```

Create `src/Lumos/Services/Interfaces/IClock.cs`:

```csharp
namespace Lumos.Services.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

Create `src/Lumos/Services/SystemClock.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

- [ ] **Step 4: Implement the active-window service and logging service**

Create `src/Lumos/Services/ActiveWindowService.cs`:

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed partial class ActiveWindowService : IActiveWindowService
{
    private static readonly HashSet<string> IgnoredExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "SearchHost.exe",
        "ApplicationFrameHost.exe"
    };

    public string? GetForegroundExecutableName()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName + ".exe";
        }
        catch
        {
            return null;
        }
    }

    public static bool IsIgnoredExecutable(string exeName) => IgnoredExecutables.Contains(exeName);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
```

Create `src/Lumos/Services/LoggingService.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class LoggingService : ILoggingService
{
    private readonly string _logPath;

    public LoggingService(string logPath)
    {
        _logPath = logPath;
    }

    public void Info(string message) => Write("INFO", message, null);
    public void Warn(string message) => Write("WARN", message, null);
    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        var line = $"{DateTimeOffset.UtcNow:O} [{level}] {message}";
        if (exception is not null)
        {
            line += $" :: {exception}";
        }

        File.AppendAllLines(_logPath, [line]);
    }
}
```

- [ ] **Step 5: Run tests to verify ignored-process behavior**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter ActiveWindowServiceTests`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/Lumos/Services/Interfaces src/Lumos/Services/ActiveWindowService.cs src/Lumos/Services/LoggingService.cs src/Lumos/Services/SystemClock.cs tests/Lumos.Tests/ActiveWindowServiceTests.cs
git commit -m "feat: add active window and logging services"
```

### Task 6: Implement the Automation Coordinator with TDD

**Files:**
- Create: `src/Lumos/Services/AutomationCoordinator.cs`
- Create: `tests/Lumos.Tests/Fakes/FakeActiveWindowService.cs`
- Create: `tests/Lumos.Tests/Fakes/FakeBrightnessProvider.cs`
- Create: `tests/Lumos.Tests/Fakes/FakeClock.cs`
- Create: `tests/Lumos.Tests/Fakes/FakeLoggingService.cs`
- Create: `tests/Lumos.Tests/Fakes/InMemoryProfileStore.cs`
- Create: `tests/Lumos.Tests/AutomationCoordinatorTests.cs`

- [ ] **Step 1: Write the failing restore-on-app-switch test**

Create `tests/Lumos.Tests/AutomationCoordinatorTests.cs`:

```csharp
using Lumos.Models;
using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class AutomationCoordinatorTests
{
    [Fact]
    public async Task RestoresSavedBrightnessForKnownApp()
    {
        var brightness = new FakeBrightnessProvider(70);
        var profiles = new InMemoryProfileStore(
            AppSettings.CreateDefault(),
            [new AppProfile { ExeName = "chrome.exe", DisplayName = "Chrome", Brightness = 30, Excluded = false, LastUpdatedUtc = DateTimeOffset.UtcNow }]);
        var activeWindow = new FakeActiveWindowService("chrome.exe");
        var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, new FakeClock(), new FakeLoggingService());

        await coordinator.TickAsync();

        Assert.Equal((byte)30, brightness.LastSetBrightness);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter RestoresSavedBrightnessForKnownApp`
Expected: FAIL with fake types or `AutomationCoordinator` missing

- [ ] **Step 3: Add the fake testing collaborators**

Create `tests/Lumos.Tests/Fakes/FakeBrightnessProvider.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Tests;

public sealed class FakeBrightnessProvider(byte brightness) : IBrightnessProvider
{
    public string ProviderName => "fake";
    public bool IsSupported => true;
    public byte CurrentBrightness { get; private set; } = brightness;
    public byte LastSetBrightness { get; private set; } = brightness;

    public Task<byte> GetCurrentBrightnessAsync(CancellationToken cancellationToken = default) => Task.FromResult(CurrentBrightness);

    public Task SetBrightnessAsync(byte value, CancellationToken cancellationToken = default)
    {
        CurrentBrightness = value;
        LastSetBrightness = value;
        return Task.CompletedTask;
    }
}
```

Create `tests/Lumos.Tests/Fakes/FakeActiveWindowService.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Tests;

public sealed class FakeActiveWindowService(string exeName) : IActiveWindowService
{
    public string? ForegroundExecutableName { get; set; } = exeName;
    public string? GetForegroundExecutableName() => ForegroundExecutableName;
}
```

Create `tests/Lumos.Tests/Fakes/FakeClock.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Tests;

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
```

Create `tests/Lumos.Tests/Fakes/FakeLoggingService.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Tests;

public sealed class FakeLoggingService : ILoggingService
{
    public List<string> Messages { get; } = [];
    public void Info(string message) => Messages.Add(message);
    public void Warn(string message) => Messages.Add(message);
    public void Error(string message, Exception? exception = null) => Messages.Add(message);
}
```

Create `tests/Lumos.Tests/Fakes/InMemoryProfileStore.cs`:

```csharp
using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.Tests;

public sealed class InMemoryProfileStore(AppSettings settings, List<AppProfile> profiles) : IProfileStore
{
    public AppSettings Settings { get; private set; } = settings;
    public List<AppProfile> Profiles { get; private set; } = profiles;
    public List<ProfileSet> ProfileSets { get; } = [];

    public Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);
    public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default) { Settings = settings; return Task.CompletedTask; }
    public Task<List<AppProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default) => Task.FromResult(Profiles);
    public Task SaveProfilesAsync(List<AppProfile> profiles, CancellationToken cancellationToken = default) { Profiles = profiles; return Task.CompletedTask; }
    public Task SaveProfileSetAsync(ProfileSet profileSet, CancellationToken cancellationToken = default) { ProfileSets.Add(profileSet); return Task.CompletedTask; }
    public Task<List<ProfileSet>> LoadProfileSetsAsync(CancellationToken cancellationToken = default) => Task.FromResult(ProfileSets);
}
```

- [ ] **Step 4: Implement the minimal coordinator**

Create `src/Lumos/Services/AutomationCoordinator.cs`:

```csharp
using Lumos.Models;
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class AutomationCoordinator
{
    private readonly IActiveWindowService _activeWindowService;
    private readonly IBrightnessProvider _brightnessProvider;
    private readonly IProfileStore _profileStore;
    private readonly IClock _clock;
    private readonly ILoggingService _loggingService;
    private string? _lastForegroundExe;

    public AutomationCoordinator(
        IActiveWindowService activeWindowService,
        IBrightnessProvider brightnessProvider,
        IProfileStore profileStore,
        IClock clock,
        ILoggingService loggingService)
    {
        _activeWindowService = activeWindowService;
        _brightnessProvider = brightnessProvider;
        _profileStore = profileStore;
        _clock = clock;
        _loggingService = loggingService;
    }

    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _profileStore.LoadSettingsAsync(cancellationToken);
        if (!settings.AutomationEnabled || (settings.PauseUntilUtc is not null && settings.PauseUntilUtc > _clock.UtcNow))
        {
            return;
        }

        var currentExe = _activeWindowService.GetForegroundExecutableName();
        if (string.IsNullOrWhiteSpace(currentExe) || string.Equals(currentExe, _lastForegroundExe, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastForegroundExe = currentExe;
        var profiles = await _profileStore.LoadProfilesAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(x => string.Equals(x.ExeName, currentExe, StringComparison.OrdinalIgnoreCase));
        if (profile is null || profile.Excluded)
        {
            return;
        }

        var currentBrightness = await _brightnessProvider.GetCurrentBrightnessAsync(cancellationToken);
        if (!settings.SkipSmallBrightnessDifferences || Math.Abs(currentBrightness - profile.Brightness) > settings.SmallDifferenceThreshold)
        {
            await _brightnessProvider.SetBrightnessAsync(profile.Brightness, cancellationToken);
            _loggingService.Info($"Restored brightness {profile.Brightness} for {currentExe}");
        }
    }
}
```

- [ ] **Step 5: Run test to verify the first coordinator behavior passes**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter RestoresSavedBrightnessForKnownApp`
Expected: PASS

- [ ] **Step 6: Add the failing manual-learning debounce test**

Append to `tests/Lumos.Tests/AutomationCoordinatorTests.cs`:

```csharp
[Fact]
public async Task LearnsManualBrightnessAfterDebounce()
{
    var brightness = new FakeBrightnessProvider(40);
    var settings = AppSettings.CreateDefault();
    var profiles = new InMemoryProfileStore(settings, []);
    var activeWindow = new FakeActiveWindowService("code.exe");
    var clock = new FakeClock();
    var coordinator = new AutomationCoordinator(activeWindow, brightness, profiles, clock, new FakeLoggingService());

    await coordinator.RecordObservedBrightnessAsync(55);
    clock.UtcNow = clock.UtcNow.AddSeconds(2);
    await coordinator.FlushPendingLearningAsync();

    Assert.Single(profiles.Profiles);
    Assert.Equal("code.exe", profiles.Profiles[0].ExeName);
    Assert.Equal((byte)55, profiles.Profiles[0].Brightness);
}
```

- [ ] **Step 7: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter LearnsManualBrightnessAfterDebounce`
Expected: FAIL with `RecordObservedBrightnessAsync` or `FlushPendingLearningAsync` missing

- [ ] **Step 8: Extend the coordinator with debounced learning**

Update `src/Lumos/Services/AutomationCoordinator.cs`:

```csharp
private byte? _pendingBrightness;
private string? _pendingBrightnessExe;
private DateTimeOffset? _pendingObservedAtUtc;

public Task RecordObservedBrightnessAsync(byte brightness)
{
    var currentExe = _activeWindowService.GetForegroundExecutableName();
    if (string.IsNullOrWhiteSpace(currentExe))
    {
        return Task.CompletedTask;
    }

    _pendingBrightness = brightness;
    _pendingBrightnessExe = currentExe;
    _pendingObservedAtUtc = _clock.UtcNow;
    return Task.CompletedTask;
}

public async Task FlushPendingLearningAsync(CancellationToken cancellationToken = default)
{
    if (_pendingBrightness is null || _pendingBrightnessExe is null || _pendingObservedAtUtc is null)
    {
        return;
    }

    if ((_clock.UtcNow - _pendingObservedAtUtc.Value) < TimeSpan.FromSeconds(1))
    {
        return;
    }

    var profiles = await _profileStore.LoadProfilesAsync(cancellationToken);
    var existing = profiles.FirstOrDefault(x => string.Equals(x.ExeName, _pendingBrightnessExe, StringComparison.OrdinalIgnoreCase));

    if (existing is null)
    {
        profiles.Add(new AppProfile
        {
            ExeName = _pendingBrightnessExe,
            DisplayName = Path.GetFileNameWithoutExtension(_pendingBrightnessExe),
            Brightness = _pendingBrightness.Value,
            Excluded = false,
            LastUpdatedUtc = _clock.UtcNow
        });
    }
    else
    {
        existing.Brightness = _pendingBrightness.Value;
        existing.LastUpdatedUtc = _clock.UtcNow;
    }

    await _profileStore.SaveProfilesAsync(profiles, cancellationToken);
    _pendingBrightness = null;
    _pendingBrightnessExe = null;
    _pendingObservedAtUtc = null;
}
```

- [ ] **Step 9: Run tests to verify coordinator behavior**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter AutomationCoordinatorTests`
Expected: PASS

- [ ] **Step 10: Commit**

```bash
git add src/Lumos/Services/AutomationCoordinator.cs tests/Lumos.Tests/Fakes tests/Lumos.Tests/AutomationCoordinatorTests.cs
git commit -m "feat: add automation coordinator"
```

### Task 7: Add Theme Service and Minimal Forms

**Files:**
- Create: `src/Lumos/Services/Interfaces/IThemeService.cs`
- Create: `src/Lumos/Services/ThemeService.cs`
- Create: `src/Lumos/UI/Controls/ThemePalette.cs`
- Create: `src/Lumos/UI/FirstRunForm.cs`
- Create: `src/Lumos/UI/SettingsForm.cs`
- Create: `src/Lumos/UI/ProfilesForm.cs`

- [ ] **Step 1: Write the failing dark-theme application test**

Append to `tests/Lumos.Tests/ThemeServiceTests.cs`:

```csharp
using Lumos.Services;

[Fact]
public void DarkThemeReturnsDarkPalette()
{
    var palette = ThemeService.GetPalette("dark");

    Assert.Equal("dark", palette.Mode);
    Assert.NotEqual(palette.BackgroundColor, palette.ForegroundColor);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter DarkThemeReturnsDarkPalette`
Expected: FAIL with `ThemeService` missing

- [ ] **Step 3: Implement the dark-theme palette and future-ready theme service**

Create `src/Lumos/UI/Controls/ThemePalette.cs`:

```csharp
using System.Drawing;

namespace Lumos.UI.Controls;

public sealed record ThemePalette(string Mode, Color BackgroundColor, Color ForegroundColor, Color AccentColor);
```

Create `src/Lumos/Services/Interfaces/IThemeService.cs`:

```csharp
using Lumos.UI.Controls;

namespace Lumos.Services.Interfaces;

public interface IThemeService
{
    ThemePalette GetPalette(string mode);
    void ApplyTheme(Control root, string mode);
}
```

Create `src/Lumos/Services/ThemeService.cs`:

```csharp
using Lumos.Services.Interfaces;
using Lumos.UI.Controls;

namespace Lumos.Services;

public sealed class ThemeService : IThemeService
{
    public static ThemePalette GetPalette(string mode) =>
        new ThemePalette("dark", Color.FromArgb(32, 32, 36), Color.Gainsboro, Color.DeepSkyBlue);

    public ThemePalette GetPalette(string mode) => GetPalette(mode);

    public void ApplyTheme(Control root, string mode)
    {
        var palette = GetPalette(mode);
        ApplyRecursive(root, palette);
    }

    private static void ApplyRecursive(Control control, ThemePalette palette)
    {
        control.BackColor = palette.BackgroundColor;
        control.ForeColor = palette.ForegroundColor;

        foreach (Control child in control.Controls)
        {
            ApplyRecursive(child, palette);
        }
    }
}
```

- [ ] **Step 4: Create minimal forms**

Create `src/Lumos/UI/FirstRunForm.cs`:

```csharp
namespace Lumos.UI;

public sealed class FirstRunForm : Form
{
    public FirstRunForm()
    {
        Text = "Lumos Setup";
        Width = 420;
        Height = 280;
    }
}
```

Create `src/Lumos/UI/SettingsForm.cs`:

```csharp
namespace Lumos.UI;

public sealed class SettingsForm : Form
{
    public SettingsForm()
    {
        Text = "Lumos Settings";
        Width = 460;
        Height = 360;
    }
}
```

Create `src/Lumos/UI/ProfilesForm.cs`:

```csharp
namespace Lumos.UI;

public sealed class ProfilesForm : Form
{
    public ProfilesForm()
    {
        Text = "Lumos Profiles";
        Width = 640;
        Height = 420;
    }
}
```

- [ ] **Step 5: Run tests to verify theme behavior**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter ThemeServiceTests`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/Lumos/Services/ThemeService.cs src/Lumos/Services/Interfaces/IThemeService.cs src/Lumos/UI src/Lumos/UI/Controls tests/Lumos.Tests/ThemeServiceTests.cs
git commit -m "feat: add minimal themed forms"
```

### Task 8: Wire Tray App, First-Run Flow, and Startup

**Files:**
- Create: `src/Lumos/UI/TrayApplicationContext.cs`
- Create: `src/Lumos/Services/StartupService.cs`
- Modify: `src/Lumos/Program.cs`

- [ ] **Step 1: Write the failing startup-registration test**

Create `tests/Lumos.Tests/StartupServiceTests.cs`:

```csharp
using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class StartupServiceTests
{
    [Fact]
    public void StartupRegistrationUsesCurrentUserRunKey()
    {
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Run", StartupService.RunKeyPath);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter StartupRegistrationUsesCurrentUserRunKey`
Expected: FAIL with `StartupService` missing

- [ ] **Step 3: Implement startup service**

Create `src/Lumos/Services/StartupService.cs`:

```csharp
using Microsoft.Win32;

namespace Lumos.Services;

public sealed class StartupService
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Lumos";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(AppName) is string;
    }

    public void SetEnabled(string executablePath, bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(AppName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
```

- [ ] **Step 4: Implement the tray application context**

Create `src/Lumos/UI/TrayApplicationContext.cs`:

```csharp
using Lumos.Models;
using Lumos.Services;
using Lumos.Services.Interfaces;

namespace Lumos.UI;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IProfileStore _profileStore;

    public TrayApplicationContext(IProfileStore profileStore)
    {
        _profileStore = profileStore;
        _notifyIcon = new NotifyIcon
        {
            Text = "Lumos",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Profiles", null, (_, _) => new ProfilesForm().Show());
        menu.Items.Add("Settings", null, (_, _) => new SettingsForm().Show());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
```

- [ ] **Step 5: Wire the app entrypoint**

Update `src/Lumos/Program.cs`:

```csharp
using Lumos.Models;
using Lumos.Services;
using Lumos.UI;

namespace Lumos;

internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        ApplicationConfiguration.Initialize();

        var dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumos");
        var profileStore = new ProfileStore(dataRoot);
        var settings = await profileStore.LoadSettingsAsync();

        if (!settings.FirstRunCompleted)
        {
            using var form = new FirstRunForm();
            form.ShowDialog();
            settings.FirstRunCompleted = true;
            await profileStore.SaveSettingsAsync(settings);
        }

        Application.Run(new TrayApplicationContext(profileStore));
    }
}
```

- [ ] **Step 6: Run tests to verify startup behavior**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter StartupServiceTests`
Expected: PASS

- [ ] **Step 7: Run the app manually**

Run: `dotnet run --project .\src\Lumos\Lumos.csproj`
Expected: first-run form appears once, then tray icon remains active

- [ ] **Step 8: Commit**

```bash
git add src/Lumos/Program.cs src/Lumos/UI/TrayApplicationContext.cs src/Lumos/Services/StartupService.cs tests/Lumos.Tests/StartupServiceTests.cs
git commit -m "feat: wire tray app and first-run flow"
```

### Task 9: Add Transition Service and Integrate It

**Files:**
- Create: `src/Lumos/Services/TransitionService.cs`
- Modify: `src/Lumos/Services/AutomationCoordinator.cs`

- [ ] **Step 1: Write the failing transition test**

Create `tests/Lumos.Tests/TransitionServiceTests.cs`:

```csharp
using Lumos.Services;
using Xunit;

namespace Lumos.Tests;

public sealed class TransitionServiceTests
{
    [Fact]
    public async Task AppliesTargetBrightnessAtEndOfTransition()
    {
        var provider = new FakeBrightnessProvider(20);
        var service = new TransitionService(provider);

        await service.ApplyAsync(60, 100, CancellationToken.None);

        Assert.Equal((byte)60, provider.CurrentBrightness);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter AppliesTargetBrightnessAtEndOfTransition`
Expected: FAIL with `TransitionService` missing

- [ ] **Step 3: Implement the transition service**

Create `src/Lumos/Services/TransitionService.cs`:

```csharp
using Lumos.Services.Interfaces;

namespace Lumos.Services;

public sealed class TransitionService(IBrightnessProvider brightnessProvider)
{
    public async Task ApplyAsync(byte targetBrightness, int durationMilliseconds, CancellationToken cancellationToken)
    {
        var current = await brightnessProvider.GetCurrentBrightnessAsync(cancellationToken);
        const int steps = 6;
        var delay = Math.Max(1, durationMilliseconds / steps);

        for (var step = 1; step <= steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = (byte)(current + ((targetBrightness - current) * step / steps));
            await brightnessProvider.SetBrightnessAsync(next, cancellationToken);
            await Task.Delay(delay, cancellationToken);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify transition behavior**

Run: `dotnet test .\tests\Lumos.Tests\Lumos.Tests.csproj --filter TransitionServiceTests`
Expected: PASS

- [ ] **Step 5: Integrate transition service into coordinator**

Update `src/Lumos/Services/AutomationCoordinator.cs` constructor and restore path:

```csharp
private readonly TransitionService? _transitionService;

public AutomationCoordinator(
    IActiveWindowService activeWindowService,
    IBrightnessProvider brightnessProvider,
    IProfileStore profileStore,
    IClock clock,
    ILoggingService loggingService,
    TransitionService? transitionService = null)
{
    _activeWindowService = activeWindowService;
    _brightnessProvider = brightnessProvider;
    _profileStore = profileStore;
    _clock = clock;
    _loggingService = loggingService;
    _transitionService = transitionService;
}

// inside TickAsync restore branch
if (settings.TransitionsEnabled && _transitionService is not null)
{
    await _transitionService.ApplyAsync(profile.Brightness, settings.TransitionDurationMilliseconds, cancellationToken);
}
else
{
    await _brightnessProvider.SetBrightnessAsync(profile.Brightness, cancellationToken);
}
```

- [ ] **Step 6: Run the full test suite**

Run: `dotnet test .\Lumos.sln`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/Lumos/Services/TransitionService.cs src/Lumos/Services/AutomationCoordinator.cs tests/Lumos.Tests/TransitionServiceTests.cs
git commit -m "feat: add smooth brightness transitions"
```

### Task 10: Manual Integration Pass and Cleanup

**Files:**
- Modify: `src/Lumos/UI/FirstRunForm.cs`
- Modify: `src/Lumos/UI/SettingsForm.cs`
- Modify: `src/Lumos/UI/ProfilesForm.cs`
- Modify: `src/Lumos/UI/TrayApplicationContext.cs`
- Modify: `src/Lumos/Program.cs`

- [ ] **Step 1: Add the minimum real controls to first-run and settings forms**

Add to `src/Lumos/UI/FirstRunForm.cs`:

```csharp
var infoLabel = new Label
{
    AutoSize = false,
    Width = 360,
    Height = 100,
    Left = 20,
    Top = 20,
    Text = "Lumos remembers brightness per app and stores settings locally."
};

var startupCheckbox = new CheckBox
{
    Left = 20,
    Top = 130,
    Width = 220,
    Text = "Start Lumos with Windows"
};

Controls.Add(infoLabel);
Controls.Add(startupCheckbox);
```

Add to `src/Lumos/UI/SettingsForm.cs`:

```csharp
var themeLabel = new Label { Left = 20, Top = 20, Width = 220, Text = "Theme: dark (light later)" };

var transitionsCheckbox = new CheckBox { Left = 20, Top = 60, Width = 220, Text = "Enable smooth transitions" };
var thresholdCheckbox = new CheckBox { Left = 20, Top = 100, Width = 260, Text = "Skip tiny brightness differences" };

Controls.Add(themeLabel);
Controls.Add(transitionsCheckbox);
Controls.Add(thresholdCheckbox);
```

- [ ] **Step 2: Add the minimum real controls to profiles form**

Add to `src/Lumos/UI/ProfilesForm.cs`:

```csharp
var grid = new DataGridView
{
    Dock = DockStyle.Fill,
    AutoGenerateColumns = true,
    AllowUserToAddRows = false,
    AllowUserToDeleteRows = false
};

Controls.Add(grid);
```

- [ ] **Step 3: Improve tray menu polish without broad UI work**

Update `src/Lumos/UI/TrayApplicationContext.cs` menu to include:

```csharp
menu.Items.Insert(0, new ToolStripMenuItem("Automation Enabled") { Checked = true, CheckOnClick = true });
menu.Items.Insert(1, new ToolStripMenuItem("Pause for 30 Minutes"));
menu.Items.Insert(2, new ToolStripSeparator());
```

- [ ] **Step 4: Run the app and verify the tray flow manually**

Run: `dotnet run --project .\src\Lumos\Lumos.csproj`
Expected:
- first-run window is basic and dark by default
- tray menu opens correctly
- settings and profiles windows open from tray
- app stays resident when windows close

- [ ] **Step 5: Run full verification**

Run:

```powershell
dotnet test .\Lumos.sln
dotnet build .\Lumos.sln -c Release
```

Expected:
- all tests pass
- release build succeeds

- [ ] **Step 6: Commit**

```bash
git add src/Lumos/UI src/Lumos/Program.cs
git commit -m "feat: complete minimal MVP shell"
```

## Self-Review

### Spec Coverage

- Per-app learning and restore: covered by Tasks 4, 6, and 9
- Provider architecture with WMI first: covered by Task 4
- Minimal dark-themed forms with future-ready theme plumbing: covered by Tasks 2, 7, and 10
- Tray-first workflow and startup: covered by Task 8 and Task 10
- Editable profiles and named profile sets: covered by Tasks 3 and 10
- Logging and failure recovery: covered by Tasks 3 and 5

No spec requirement is currently uncovered, but the concrete settings/profile form wiring remains intentionally minimal, matching the latest UI scope decision.

### Placeholder Scan

- No `TODO`/`TBD` placeholders remain.
- All tasks include exact files, commands, and code snippets.
- The initial ignored-process list is intentionally small and explicit; expansion can happen only if real behavior requires it.

### Type Consistency

- `AppSettings.ThemeMode` is consistently used as `dark`/`light`
- `IBrightnessProvider` contract is consistent across manager, transition service, and coordinator
- `IProfileStore` methods are consistent across file and in-memory implementations

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-25-lumos-brightness-memory-implementation.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
