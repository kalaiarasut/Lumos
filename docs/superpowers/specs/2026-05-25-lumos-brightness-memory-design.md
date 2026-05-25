# Lumos Brightness Memory Design

## Goal

Build a lightweight Windows tray application that automatically learns and restores internal-display brightness per application with minimal CPU and RAM overhead. The application should feel invisible in daily use: the user changes brightness normally, Lumos learns the final value for the active app, and later restores that value when the app regains focus.

## Product Summary

Lumos runs as a per-user background utility. It detects the current foreground application, monitors laptop-panel brightness, and stores learned per-application brightness preferences locally. On future app switches, it restores the remembered brightness for that app using a smooth transition. Profiles are editable through a small tray-accessible UI, but the main workflow is automatic.

Examples:

- `chrome.exe` -> `20%`
- `code.exe` -> `40%`
- `vlc.exe` -> `10%`

If an app has no learned profile yet, Lumos does nothing and leaves the current brightness unchanged.

## Technical Stack

- Language: `C#`
- UI Framework: `WinForms`
- Runtime Shape: per-user tray application
- Primary brightness backend: `WMI`
- Storage: JSON files under `%AppData%\Lumos\`

This stack is correct for the MVP and near-term roadmap. The key architectural precaution is to isolate brightness access behind a provider abstraction from day one so future display backends can be added without rewriting the learning, restore, UI, or profile logic.

## UI Scope Decision

The desktop window UI is intentionally minimal in MVP because it will be redesigned later. Implementation should avoid spending time on visual polish for forms beyond a basic, functional presentation.

UI priorities:

- The tray experience should be the most polished part of the product.
- First-run, settings, and profiles windows should be basic and utilitarian.
- A simple dark theme is acceptable as the default if it is easy to implement cleanly.
- No custom design system, advanced styling work, or form-level UX experimentation should be part of MVP.

## Non-Goals for MVP

- External monitor support
- DDC/CI support
- HDR-aware behavior
- Ambient light / adaptive brightness
- OLED protection logic
- Per-window-title brightness memory
- Auto-forcing a default fullscreen brightness for unknown apps

Fullscreen apps such as VLC are still supported because learning and restore are keyed by application executable, not by window mode. If `vlc.exe` has a saved profile, it will restore whether VLC is windowed or fullscreen.

## Core Requirements

### 1. Foreground Application Detection

Lumos must continuously identify the currently focused app using standard Windows APIs:

- `GetForegroundWindow()`
- `GetWindowThreadProcessId()`

The tracked app identity in MVP is the executable name, for example:

- `chrome.exe`
- `code.exe`
- `vlc.exe`

Profiles are keyed by executable name rather than full path to keep behavior stable across app updates and installations.

### 2. Automatic Learning

When the user manually changes brightness while an app is in the foreground:

- Lumos detects the new brightness value.
- Lumos waits for the brightness to settle for a short debounce window.
- Lumos saves the final settled value for the current foreground executable.

Learning rules:

- Any brightness source counts as manual input if the change was not initiated by Lumos itself.
- Manual changes always override automation.
- If the user changes brightness while Lumos is restoring brightness, the restore is cancelled immediately and the manual value becomes the new learned value after debounce.

### 3. Automatic Restore

When the foreground application changes:

- Lumos checks whether that executable has a saved brightness profile.
- If no profile exists, Lumos does nothing.
- If a profile exists, Lumos restores the saved brightness using a smooth transition.

Restore behavior:

- Default behavior is exact restore to the saved value.
- Settings expose an optional threshold to skip restores when the difference is very small.
- Smooth transitions are part of MVP, not deferred.

### 4. Tray-First Background Utility

The application should:

- start with Windows on a per-user basis
- live in the system tray
- consume minimal resources
- require almost no interaction after initial setup

Closing settings windows should close the window itself while keeping the tray application running.

### 5. First-Run Setup

On first launch only, Lumos shows a setup/consent screen that:

- explains what the app monitors
- explains that data is stored locally
- allows enabling startup
- offers a retry/elevated flow only if brightness control fails

After first run:

- later launches skip onboarding
- the app starts directly into tray mode

### 6. Editable Profiles

Profiles must be editable from the UI. The user can:

- rename a profile for display purposes
- change its saved brightness manually
- delete it
- exclude it from automation
- import/export profile sets
- save/load named profile sets

If a profile is edited and its app is currently active, the new brightness can be applied immediately.

## Architecture

### High-Level Components

#### 1. Active Window Service

Responsibility:

- Poll the current foreground window and resolve the active executable name.
- Filter ignored/transient system processes.

Outputs:

- current foreground app identity
- app-switch notifications for the coordinator

#### 2. Brightness Provider Interface

Responsibility:

- Provide a stable contract for getting and setting laptop brightness.

Initial contract:

- `ProviderName`
- `IsSupported`
- `GetCurrentBrightness()`
- `SetBrightness(byte value, CancellationToken token)`
- `GetSupportedTargets()`

Initial implementation:

- `WmiBrightnessProvider`

This abstraction is mandatory even if WMI is the only concrete provider in MVP. It is the main future-proofing measure for machines where WMI support is inconsistent.

#### 3. Brightness Provider Manager

Responsibility:

- Discover supported providers
- Choose the active provider
- Expose a unified brightness service to the rest of the app

Initial behavior:

- Prefer WMI if supported.
- If no supported provider is available, surface a recoverable warning in the UI and logging system.

#### 4. Transition Service

Responsibility:

- Perform smooth brightness changes during restore.
- Cancel in-progress transitions on app switches or manual overrides.

Design:

- short animation duration
- stepped interpolation from current brightness to target brightness
- cancellation-aware

#### 5. Profile Store

Responsibility:

- Load and save settings
- Load and save app brightness profiles
- Import/export profile sets
- Manage named profile sets

Storage location:

- `%AppData%\Lumos\settings.json`
- `%AppData%\Lumos\profiles.json`
- `%AppData%\Lumos\profile-sets\*.json`

#### 6. Automation Coordinator

Responsibility:

- Central state machine for app switching, restore decisions, learning decisions, pauses, and suppression of self-triggered changes.

This service owns the most important behavior:

- distinguish Lumos-initiated brightness writes from user-initiated writes
- debounce learning after manual changes
- restore on app switch
- cancel restore when manual input appears
- suppress accidental relearning of Lumos's own restored value

#### 7. Startup Service

Responsibility:

- Manage per-user startup registration

Scope:

- per-user only

#### 8. Logging Service

Responsibility:

- Maintain a small rotating local log
- support normal and verbose logging

Decision:

- Keep minimal rotating logs enabled by default because this is the best debugging/support tradeoff.

## State and Data Model

### Settings

Settings should include:

- automation enabled
- pause-until timestamp
- startup enabled
- transition enabled
- transition duration
- skip-small-difference enabled
- small-difference threshold
- verbose logging enabled
- first-run-completed flag
- exclusion list
- active profile-set name

### App Profile

Each app profile should contain:

- executable name
- display name
- brightness value
- excluded flag
- last updated timestamp

Example:

```json
{
  "exeName": "chrome.exe",
  "displayName": "Chrome",
  "brightness": 30,
  "excluded": false,
  "lastUpdatedUtc": "2026-05-25T12:00:00Z"
}
```

## Runtime Behavior

### Polling Model

Foreground polling:

- every `~300ms`

Brightness polling:

- every `~500ms`

These values are fast enough for responsive app switches while remaining low-cost on CPU.

### App Switch Flow

1. Active window service detects a new foreground executable.
2. Automation coordinator ignores the event if automation is paused or disabled.
3. Coordinator checks exclusion rules and transient-process filters.
4. Coordinator loads any saved profile for the new executable.
5. If no profile exists, no brightness change occurs.
6. If a profile exists, coordinator checks the restore-threshold setting.
7. If restore is required, transition service applies brightness to the saved value.
8. Coordinator marks the resulting brightness change as self-initiated so it does not get relearned accidentally.

### Manual Learning Flow

1. Brightness watcher sees a brightness change.
2. Coordinator determines whether the change came from Lumos or from outside Lumos.
3. If the change was Lumos-initiated, it is ignored for learning.
4. If the change was external/manual, any in-progress restore is cancelled.
5. Coordinator starts or refreshes a debounce timer.
6. When the brightness stabilizes, the final value is saved for the current foreground executable.
7. UI and logs update accordingly.

### Pause Behavior

`Pause for 30 minutes` pauses both:

- automatic restore
- automatic learning

This keeps the app fully out of the way when paused.

## UX Design

### Tray Menu

Tray menu should include:

- Automation on/off
- Pause for 30 minutes
- Resume now
- Profiles
- Settings
- Current active app
- Current brightness
- Exit

### First-Run Window

The first-run UI should be basic and functional. It should include:

- concise explanation of how Lumos works
- local-storage/privacy summary
- startup toggle
- a "test brightness access" action
- fallback guidance if brightness control is unavailable

If brightness writes fail:

- show a clear warning
- offer a retry path
- offer elevation only as a fallback action

### Settings Window

The settings surface should be minimal and functional. Minimum controls:

- enable/disable automation
- startup toggle
- enable/disable smooth transitions
- transition duration
- enable/disable small-difference skipping
- threshold value
- verbose logging toggle
- exclusions management
- profile set load/save/import/export actions

### Profiles Window

The profiles editor should provide the following in a basic, utilitarian layout:

- list of learned apps
- editable display names
- editable brightness values
- exclude toggle
- apply/test action
- delete action
- import/export actions
- save current set as named profile set
- load an existing named profile set

## Default Filters and Exclusions

Lumos should ship with a built-in ignored-process list for transient or system-owned windows such as:

- `SearchHost.exe`
- `ApplicationFrameHost.exe`
- shell/transient utility windows where app-based learning would be noisy

The initial ignored-process list should be seeded with the known transient/system processes above and kept configurable in settings. The product requirement is clear: transient system windows must not cause spurious learning or restores, and users must be able to add or remove exclusions manually.

`explorer.exe` should not be hardcoded as ignored; users may want a desktop/file-manager brightness preference. It can be excluded manually if desired.

## Failure Handling

### Brightness Control Unavailable

If no brightness provider is supported:

- automation cannot run
- tray and settings remain available
- user sees a persistent warning state
- logging records the failure

### Repeated Write Failures

If brightness set attempts fail repeatedly:

- keep the app running
- continue app monitoring
- surface a warning in tray/settings
- allow retry/testing
- allow elevated relaunch only as a fallback path

### Corrupt Profile Files

If settings or profiles JSON is corrupt:

- back up the bad file
- reset to a clean default file
- warn the user in the UI
- retain logs for diagnosis

## Performance Targets

- CPU: near `0%` at idle
- RAM: preferably `20MB-50MB`
- Startup: under `2 seconds`

The app should favor simple polling plus careful state guards over heavier system integration.

## Testing Strategy

Testing should cover:

- profile load/save behavior
- named profile set import/export
- debounce learning logic
- app-switch restore logic
- restore-threshold logic
- suppression of self-triggered relearning
- transition cancellation on manual override
- exclusion handling
- corrupted-settings recovery

Implementation should use interfaces around:

- active window detection
- brightness provider
- clock/timers
- storage

This allows the automation coordinator and persistence logic to be tested without real WMI or live Windows focus changes.

Manual validation should cover:

- learning Chrome/VS Code/VLC values
- rapid Alt-Tab switching
- fullscreen VLC restore
- pause/resume behavior
- first-run onboarding
- startup behavior after sign-in
- failure path when brightness control is unavailable

## Scope Check

This design is still appropriately scoped for a single implementation plan. It contains multiple components, but they form one cohesive desktop utility rather than unrelated subsystems. The provider abstraction is included now to avoid future rewrites, but only one concrete provider is required for MVP.

## Final Recommendation

Proceed with:

- `C#`
- `WinForms`
- tray-first Windows utility
- provider architecture from day one
- `WMI` as the first concrete brightness backend

This is the best fit for the desired product: lightweight, native-feeling, low-overhead, and prepared for future brightness-backend issues without requiring core-logic rewrites.
