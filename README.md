# Lumos

![Lumos logo](logo.png)

Lumos is a lightweight Windows tray application that remembers and restores laptop display brightness per application.

It watches the foreground application, learns the brightness level you choose manually, and restores that level when you return to the same app. Profiles, settings, and logs stay local on your machine.

> Current version: `0.3.0`

## Why Lumos Exists

Different apps often need different brightness levels. A browser, code editor, video player, and document viewer can all feel better at different display brightness settings. Lumos makes that preference automatic without accounts, cloud sync, or a heavy background service.

Examples:

| Application | Learned brightness |
| --- | --- |
| `chrome.exe` | `25%` |
| `Code.exe` | `45%` |
| `vlc.exe` | `15%` |

If an app has no learned profile yet, Lumos leaves the current brightness unchanged.

## Features

- Per-application brightness memory keyed by executable name.
- Automatic learning from manual brightness changes.
- Automatic restore when switching back to a known app.
- Smooth brightness transitions with cancellation support.
- System tray controls for automation, pause, resume, status, logs, and profile access.
- WPF dashboard for settings and profile management.
- Named profile sets stored as local JSON files.
- Current-user Windows startup registration.
- Single-instance guard to prevent duplicate tray processes.
- Local logging and corrupt JSON recovery.
- xUnit test coverage for core automation, persistence, startup, transitions, and provider selection.

## How It Works

Lumos runs as a tray-first desktop utility.

1. `ActiveWindowService` polls the foreground window every 300 ms using Windows APIs.
2. `AutomationCoordinator` checks whether the active executable has a saved brightness profile.
3. `WmiBrightnessProvider` reads and writes internal display brightness through WMI.
4. `TransitionService` applies smooth brightness changes when restore is enabled.
5. Manual brightness changes are recorded, debounced, and saved to the active app profile.
6. `ProfileStore` persists settings, profiles, and profile sets under `%APPDATA%\Lumos`.

Automation pauses both learning and restore. Manual brightness changes during an automatic restore cancel the restore and become the new learned value after debounce.

## Supported Platform

Lumos targets Windows laptops with WMI-backed internal brightness control.

Minimum development/runtime target:

- Windows
- .NET 8
- Internal laptop display exposed through `root\wmi`
- x64 for packaged release builds

Current implementation limits:

- External monitor brightness control is not implemented.
- DDC/CI, HDR-aware behavior, ambient-light behavior, and per-window-title profiles are not implemented.
- Brightness provider support currently ships with WMI only.

## Installation

The repository includes an Inno Setup installer definition and a release publishing script.

For users, the intended installation path is a release installer named like:

```text
LumosSetup-0.3.0.exe
```

The installer is configured to publish releases through:

- [GitHub releases](https://github.com/kalaiarasut/Lumos/releases)
- [GitHub issues](https://github.com/kalaiarasut/Lumos/issues)

Assumption: release artifacts may not exist yet in a fresh clone. If no installer is available, build from source using the development steps below.

## Usage

Launch `Lumos.exe`. The app starts in the system tray.

Tray actions include:

- Enable or disable automation.
- Pause automation for 30 minutes.
- Resume automation immediately.
- View active app, current brightness, and selected provider.
- Open Profiles.
- Open Settings.
- Forget the current app profile.
- Test brightness control.
- Open logs or the local data folder.
- Exit Lumos.

The settings dashboard currently exposes:

- Automation enabled
- Start with Windows
- Smooth transitions
- Light or dark theme
- Skip tiny brightness differences
- Small-difference threshold
- Manual restore cooldown

The profiles dashboard currently exposes:

- Learned app profiles
- Brightness sliders
- Exclude toggles
- Apply selected profile
- Delete selected profile
- Save all profiles
- Save and load named profile sets

## Local Data and Privacy

Lumos does not require an account and does not upload profile data.

Data is stored locally:

| Path | Purpose |
| --- | --- |
| `%APPDATA%\Lumos\settings.json` | User settings |
| `%APPDATA%\Lumos\profiles.json` | Learned app brightness profiles |
| `%APPDATA%\Lumos\profile-sets\*.json` | Named profile sets |
| `%APPDATA%\Lumos\logs\lumos.log` | Local diagnostic log |

If settings or profile JSON is corrupt, Lumos moves the bad file aside with a `.corrupt.<timestamp>` suffix and starts from defaults.

## Architecture

```text
src/Lumos
  Models
    AppSettings.cs
    AppProfile.cs
    ProfileSet.cs
  Services
    ActiveWindowService.cs
    AutomationCoordinator.cs
    BrightnessProviderManager.cs
    ProfileStore.cs
    StartupService.cs
    TransitionService.cs
    WmiBrightnessProvider.cs
  UI
    TrayApplicationContext.cs
    WPF
      DashboardWindow.xaml
      SettingsView.xaml
      ProfilesView.xaml
  ViewModels
    MainViewModel.cs
    SettingsViewModel.cs
    ProfilesViewModel.cs
tests/Lumos.Tests
  Core behavior and service tests
installer
  Inno Setup definition and install notes
scripts
  Release publishing script
```

Core components:

| Component | Responsibility |
| --- | --- |
| `TrayApplicationContext` | WinForms tray lifecycle, menu actions, polling timers, WPF dashboard launch |
| `AutomationCoordinator` | App-switch restore logic, manual learning, pause handling, suppression of self-triggered changes |
| `ActiveWindowService` | Foreground executable detection through `user32.dll` |
| `IBrightnessProvider` | Provider contract for brightness backends |
| `WmiBrightnessProvider` | WMI implementation for laptop-panel brightness |
| `ProfileStore` | JSON persistence for settings, profiles, and profile sets |
| `StartupService` | Current-user startup registration through the Windows Run key |
| `TransitionService` | Stepped brightness interpolation with cancellation |

## Development

Clone the repository and restore dependencies:

```powershell
git clone https://github.com/kalaiarasut/Lumos.git
cd Lumos
dotnet restore .\Lumos.sln
```

Run tests:

```powershell
dotnet test .\Lumos.sln
```

Build a release configuration:

```powershell
dotnet build .\Lumos.sln -c Release
```

Run the app locally:

```powershell
dotnet run --project .\src\Lumos\Lumos.csproj
```

Stop the app from the Lumos tray menu with `Exit`.

## Release Packaging

The release script performs three steps:

1. Runs the full test suite.
2. Publishes a self-contained `win-x64` build to `artifacts\publish\Lumos`.
3. Builds an Inno Setup installer into `artifacts\installer` when `iscc.exe` is available.

Run:

```powershell
.\scripts\publish-release.ps1
```

If Inno Setup is not installed, the script leaves the published app output ready and prints a warning. Install Inno Setup 6 or 7, then rerun the script to create the installer.

## Testing

The test project uses xUnit and fake service implementations so the core automation behavior can be tested without real WMI access or live Windows focus changes.

Covered areas include:

- Bootstrap and project load.
- Active-window ignored executables.
- Provider selection.
- Profile and settings persistence.
- Corrupt settings recovery.
- Startup registry path.
- Smooth transition completion.
- Restore for known apps.
- Manual brightness learning after debounce.
- Disabled and paused automation behavior.
- Manual override during transition.
- View model behavior.

Run all tests with:

```powershell
dotnet test .\Lumos.sln
```

## Troubleshooting

### No brightness provider is available

Lumos currently depends on Windows WMI brightness APIs for internal laptop panels. If the tray status says no provider is available, the tray and settings UI can still open, but automation cannot run.

Try:

- Confirm the machine is a Windows laptop with an internal display.
- Use the tray action `Test Brightness Control`.
- Check `%APPDATA%\Lumos\logs\lumos.log`.

### Brightness changes are not being learned

Check whether automation is disabled or paused. Learning is intentionally disabled while automation is inactive.

### Lumos starts twice

The app has a single-instance guard. If a second launch is attempted, it shows a message that Lumos is already running.

### Settings reset unexpectedly

Check `%APPDATA%\Lumos` for files ending in `.corrupt.<timestamp>`. Lumos creates these when a JSON file cannot be parsed.

## Contributing

Contributions should keep Lumos small, local-first, and tray-first.

Before submitting changes:

```powershell
dotnet test .\Lumos.sln
dotnet build .\Lumos.sln -c Release
```

Preferred implementation guidelines:

- Keep brightness backends behind `IBrightnessProvider`.
- Keep automation behavior in `AutomationCoordinator`.
- Use interfaces for behavior that needs deterministic tests.
- Avoid storing user-specific paths or machine details in profiles.
- Do not add cloud services or telemetry without an explicit product decision.

## License

No license file is currently present in this repository. Add a license before distributing or accepting external contributions.
