# SleepHunter

<img src="src/SleepHunter.App/SleepHunter.png" width="32" height="32" alt="SleepHunter icon" />
<img src="src/SleepHunter.Updater/SleepHunter-Updater.png" width="32" height="32" alt="SleepHunter Updater icon" />

SleepHunter is a Windows automation companion for the
[Dark Ages](https://www.darkages.com) game client. It helps manage multiple
characters and automate repetitive skill, spell, and flowering routines without
taking over the user's mouse or keyboard.

## Highlights

- Per-client automation for skills, spell queues, and flower queues
- Live queue editing, drag-and-drop reordering, rotation modes, and target-level goals
- Automatic Fas Spiorad use, class-aware staff switching, and alternate-character targeting
- Global character hotkeys for starting, pausing, and resuming macros
- Coherent live character, map, inventory, equipment, group, cooldown, dialog, and nearby-entity observations
- Automatic safety behavior while typing, changing maps, switching client panels, or handling dialogs
- Inventory and equipment icons, slot details, cooldown feedback, runtime diagnostics, and customizable color themes
- Versioned `.sh4x` macro files with import support for legacy `.sh4` files
- Built-in update checks and a separate updater

## Requirements

- A 64-bit Windows version supported by
  [.NET 10](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)
- The [.NET 10 Desktop Runtime for x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- A supported 32-bit Dark Ages 7.41 client, or a compatible custom executable
  configured through `data/ClientLayout.xml`

SleepHunter is published for `win-x64`. The Dark Ages client it observes remains
a 32-bit process.

## Installation

1. Download the latest archive from
   [GitHub Releases](https://github.com/ewrogers/SleepHunter4/releases).
2. Extract every file to a folder of your choice.
3. Run `SleepHunter.exe`.
4. Set the Dark Ages executable under `Settings > Game Client` if it was not
   detected automatically.
5. Review the options under `Settings > Patches` before launching a client from
   the SleepHunter toolbar.

SleepHunter can check for updates under `Settings > Updates`. Updating preserves
the existing `Settings.xml` file while replacing application and runtime data
files with the release versions.

## Automation

Each logged-in client has an independent macro configuration and runtime.
SleepHunter can interleave ready skills, cast queued spells using the selected
rotation mode, and distribute mana through Lyliac Plant or Lyliac Vineyard.
Queues and skill toggles can be changed while automation is running, and each
complete edit is applied without stopping the macro.

The combined macro toolbar button changes between Start, Pause, and Resume.
Stopping resets temporary runtime state such as flower intervals, but it does
not remove configured skills, spells, or flower targets.

The selected client status bar shows healthy observations, expected transition
waits, and actionable failures. Its details view includes recent read timing,
captured state, and nested error information for troubleshooting.

## Client launcher patches

Patches are applied only when SleepHunter launches a new client. Changing a
setting does not modify clients that are already running.

| Patch | Default | Purpose |
| --- | --- | --- |
| Allow Multiple Instances | Enabled | Allows more than one Dark Ages client to run |
| Skip Intro Video | Enabled | Opens the login flow without playing the intro |
| Suppress Login Notification | Enabled | Removes the login notice and transfer delay |
| Apply Modifiers Key Fix | Enabled | Prevents Alt, Ctrl, or Shift from remaining stuck after focus changes |
| Show Ground Items with Alt | Enabled | Reveals up to 255 ground items as translucent hints while Alt is held |
| Improved Auto-Follow | Enabled | Follows a player or monster without attacking while Shift and right-click are used |
| No Foreground Walls | Disabled | Hides foreground wall tiles |
| Show Item Quantities in Dialogs | Enabled | Adds stack quantities to merchant and storage item names |
| Make Exchange Window Draggable | Enabled | Allows the exchange window to be repositioned |
| Show Exchange Results in Message Bar | Disabled | Moves accepted and cancelled exchange results to the floating message bar |

Runtime hooks are signature-checked. If the configured executable cannot be
verified or a required patch fails, SleepHunter stops the suspended launch
instead of running a partially patched client.

See [Patches Settings](./docs/src/settings/patches.md) for the full option
reference.

## Client compatibility

SleepHunter uses one verified layout in `data/ClientLayout.xml` for process
detection, memory addresses, and patch metadata. Compatible custom clients can
use updated values in that mapping, but layouts must be based on documented
client behavior or reproducible live observations.

Live reads are bounded and checked for consistency because the game client can
change its own collections while SleepHunter is observing them. Compact
inventory, skill, spell, and cooldown readings remain available as compatibility
fallbacks when richer client panes cannot be read safely.

The interop layer is the application's only live client reader. It publishes
coherent snapshots to the automation runtime, and the WPF application projects
those same snapshots into the client list, vitals, inventory, equipment, skill,
and spell views. The application does not run a second client-reading path.
Direct process memory access remains only in the signature-checked client
launcher patch flow described above.

Live-client verification guidance is maintained in
[Live Smoke Testing](./SMOKE_TESTING.md).

## Documentation

- [User Manual](https://ewrogers.github.io/SleepHunter4/)
- [Documentation Source](./docs)
- [Release Notes](./CHANGELOG.md)
- [Release Process](./RELEASING.md)

## Development

Development requires Windows and the .NET 10 SDK selected by `global.json`.
Open `SleepHunter.sln` in a compatible .NET IDE, or use the repository root:

```powershell
dotnet restore SleepHunter.sln
dotnet build SleepHunter.sln --configuration Release --no-restore
dotnet test SleepHunter.sln --configuration Release --no-build
dotnet run --project src/SleepHunter.App/SleepHunter.App.csproj --configuration Release
```

Repository layout:

- `src/SleepHunter.App/` contains the WPF application
- `src/SleepHunter.Updater/` contains the updater
- `src/SleepHunter.Runtime/` contains deterministic automation state and planning
- `src/SleepHunter.Interop/` contains guarded client observation and input
- `src/SleepHunter.Persistence/` contains versioned macro persistence
- `tests/` contains the NUnit test projects
- `data/` contains client mappings, themes, and ability metadata
- `docs/` contains the mdBook user manual

The application project is named `SleepHunter.App`, while the shipped product
and executable remain `SleepHunter` and `SleepHunter.exe`.

The client list and runtime automation state use MVVM view models and observable
snapshot projections. The application is not yet fully MVVM. Code-behind remains
for WPF window and Win32 host integration, and command or interaction logic
still exists in the main window, metadata, settings, target, editor, and update
dialogs. Moving that interaction logic into view models is the remaining UI
migration boundary. New presentation behavior should be implemented in view
models, bindings, templates, converters, or services rather than expanding that
code-behind.

Changes that affect live client mappings, input, patches, or automation should
include deterministic tests and still be verified against the supported 7.41
client. Read [AGENTS.md](./AGENTS.md) before contributing.

## License

SleepHunter is available under the [MIT License](./LICENSE).
