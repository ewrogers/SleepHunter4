# Changelog
All notable changes to this library will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.8.2] - Unreleased

### Added

- "Browse" button for selecting client path with open file dialog

### Changed

- `Flowering` tab is now only shown in USDA clients
- Tweak the color theme dropdown layout
- Spell queue automatically opens when selected a character with queued spells

## [4.8.1] - 2023-06-22

### Added

- Gold display to inventory (last slot)
- `Features` tab for client-specific feature options (per-character)
- Feature flag support for client-specific functionality in `ClientVersion` definitions

### Changed

- Client version `7.41` is now renamed `USDA 7.41` for clarity

### Fixed

- Fas spiorad bugs (needlessly cast at low mana)
- Better spellbook cooldown updates

## [4.8.0] - 2023-06-21

### Added

- `Inventory` tab to view items (names only for now)
- Inventory grid display options under `User Interface` settings
- `Signature` definition for `ClientVersion`, which allows version to be detected by signature bytes instead of hash
- `ExecutableName` and `WindowClassName` properties for `ClientVersion` to support other clients
- Client version for `Zolian 9.1.1` memory offsets ([Zolian Server](https://www.thebucknetwork.com/Zolian))

### Changed

- `UserSettings` is now version `1.6`
- Launched clients now detect version based on the new signature definitions
- Process manager can detect other clients based on version definitions
- HP/MP formatting threshold increased to 10k for "thousands" shorthand
- `User Settings` dialog is now larger

### Removed

- `Value` in `ClientVersion`, as it was never used
- `Hash` in `ClientVersion`, now using signature-based detection instead 

### Fixed

- Parsing of skills/spell names with no level text

## [4.7.0] - 2023-06-16

### Added

- Spell rotation combo box in Spell Queue for per-character setting
- Spell rotation character setting is preserved in saved state
- Spell cooldown indicator in Spell Queue
- New option for `Skip Spells on Cooldown` for `Spell Macros` (default is `Enabled`)
- Spells on cooldown will be skipped, even in no rotation/singular order (when enabled)
- More accessibility key for checkboxes in `Spell Macro` settings

### Changed

- `Spell Rotation Mode` renamed `Default Spell Queue Rotation` to better describe it can be overriden
- `UserSettings` are now version `1.5`
- Now format health/mana using `k` and `m` suffixes for thousands/millions (ex: `256k`, `1.2m`)

### Fixed

- Some staff line changes
- Better spell queue rotation handling

## [4.6.1] - 2023-06-03

### Added

- Status icons now next to character name (when running/paused)

### Changed

- Small UI tweaks on character list text spacing
- Adjusted min size on the character list to accomodate new status icon
- Will look in a few places for the default DA client path

### Removed

- `DirectDrawCompatibilityFix` option and support (it was causing side effects for some users)
- **TO REMOVE:** Delete `ddraw.dll` and the `DDrawCompat-Darkages.ini` files from your DA client folder

### Fixed

- Marked `Assail`, `Assault`, and `Clobber` skills as assail types
- Clear macro status on stop (fixes "Assailing" being displayed when stopped)
- `Execute` skill now also waits for < 2% hp
- Spell queue levels not updating until next cast

## [4.6.0] - 2023-05-31

### Added

- `DirectDrawCompatibilityFix` option for fixing flickering mouse cursor ([DDrawCompat](https://github.com/narzoul/DDrawCompat) repo)

### Changed

- `UserSettings` are now version `1.4`

## [4.5.5] - 2023-05-21

### Added

- Many missing spells for Temuair
- Many missing spells for Medenia (AB50+)
- Many missing skills for Temuair
- Many missing skills for Medenia (AB50+)

### Fixed

- Spell lines and mana costs for some spells
- Keyboard navigation via tab in some UI elements

## [4.5.4] - 2023-05-19

### Fixed

- Skill cooldown memory reading inconsistencies

## [4.5.3] - 2023-05-18

### Fixed

- Process memory scanning on 64-bit, cooldowns should be more reliable now
- Reset cooldown pointer on re-log same client instance

## [4.5.2] - 2023-05-18

### Added

- Missing staves for all Medenia classes (AB 50+)
- Missing staves for bards (AB 70+)
- Missing staves for summoners (AB 70+)

### Fixed

- Cooldown detection fixed on 64-bit computers
- Macro toolbar state now updates more reliably (map/location change)
- Spell queue now hides when last character logs out
- Non-integer window scaling now supported (ex: 150%, 175%, etc)

## [4.5.1] - 2023-04-18

### Added

- Can dismiss Spell & Flower Target dialogs via `Escape` key

### Fixed

- Hide `No Target` option for spells that require one
- Hide the `Mouse Offset` for `No Target` spells
- Hide the `Mouse Offset` for `Screen Position` flower target

## [4.5.0] - 2023-04-18

### Added

- CPU info in `Settings-About` tab
- .NET version displayed in `Settings->About` tab

### Changed

- Now built against newer [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.00) instead of older .NET Framework 4.8.1 runtime
- Removed build date from `Settings->About` tab

## [4.4.2] - 2023-04-18

### Changed

- Client versions no longer reset to defaults, file is required
- Client versions no longer auto-save to file on close
- Client versions will warn on startup when not found, disable start client button
- Themes no longer auto-save to file on close
- Themes will fallback to default style on error
- Metadata files no longer auto-save to files on close
- Status bar has been removed and made into thinner border + resize grip

### Fixed

- Character sorting by login time
- Character sorting not updating
- Not showing all clients when debug option was checked on startup

## [4.4.1] - 2023-04-16

### Added

- `Show All Processes` debug option in `Settings->Debug`
- Login timestamp to players
- Client sort order option in `Settings->User Interface`, defaults to login time
- Get client start time from Win32 kernel

### Changed

- `UserSettings` are now version `1.3`
- Non-logged in game clients are now hidden by default (use new debug option to show)
- Clients now default sort by login time (oldest to newest)
- Tweak layout of metadata editor windows

### Fixed

- Disable spell queue remove buttons on startup (when empty)
- SleepHunter window title not changing sometimes
- Disable skill/spell tabs when no client
- Disable spell queue when no client

## [4.4.0] - 2023-04-15

### Added

- `Debug` tab in `Settings` window
- More skill & spell metadata
- MP cost display in spell target dialog
- Height resize animations to spell and flower dialogs when selecting target types
- [User Manual](https://ewrogers.github.io/SleepHunter4/) hyperlink within application (still WIP)
- On first run the application will ask the user if they wish to open the user manual docs in the browser
- User manual link in settings window (below all tabs)
- Spell queue toggle button
- `Stop All` toolbar button

### Changed

- All new color themes
- `UserSettings` are now version `1.2`
- Moved debug logging option to new `Debug` section in `Settings` window
- Accessibility shortcuts for tabs in the `Settings` window
- Better control alignment and layout in the `Settings` window sections
- Shortened toolbar button text as `Start Macro`, `Pause Macro`, `Stop Macro` instead
- Metadata editor is now launched from toolbar directly (instead of `Settings->General`)
- Improved wording on auto-save macro state setting
- Minimum flower "less than mana" threshold is 1 mana (instead of zero)
- Numeric inputs can now have prefix/unit contextual decorators
- Redesigned layout of spell and flower target dialogs
- Flower queue now shows a timer instead
- Increased padding on flowering options under the main window tab
- Spell queue now highlights icon in white instead of color to be consistent with skill macros
- Renamed `Tile Radius` to `Tile Area` target type for more clairity
- Renamed `Absolute X/Y` to `Screen Position` target type for more clarity
- Screen coordinate targeting capped to 1280x960, minimum now zero for each dimension
- Slightly narrower dropdown button
- Tooltips open faster
- Spell queue now has a placeholder on the right side
- Show 'No Target' in spell queue for no target spells

### Removed

- Data file error dialogs on launch
- `Relative Coordinates` target type, as it is redundant with `Self` and x/y offsets
- `Rainbow Mode` as it is not very useful, visually
- `Reset Themes` button in `Settings->User Interface` section
- `Reset Version` button in `Settings->Game Client` section
- Missing spell indicator warning (now shows as zero MP)
- Progress bar in flower queue
- Status bar 'Ready' text

### Fixed

- Disable start/pause/stop buttons on app load, until client login
- Disable start/pause/stop buttons on client logout
- Numeric inputs now use regex and allow much better text input of values
- Numeric input boxes now validate/update on lost focus
- Spell queue "disabled" selected text is now white instead of gray on gray
- Double-click actions only respond to left-click now
- Select default them when invalid settings
- Spell queue will only open when adding a spell (not when switching tabs)

## [4.3.0] - 2023-04-11

### Added

- SleepHunter can now update the Updater prior to installing new versions
- Visual separate between vertical tabs (settings window)
- New standalone Updater (no references to main app)
- Basic MVVM code for Updater

### Changed

- Main window title font reduced
- Font size reduced throughout most text
- HP/MP font size slightly increased
- Dropdown and text input boxes wider in most places
- Spell queue current/max level font size slightly increased
- Slightly adjusted UI background and text colors for contrast

### Fixed

- Better file handling for potentially missing files when client path is invalid
- Numeric up/down now highlights border when it has focus

## [4.2.1] - 2023-04-11

### Added

- Updater now has a retry button on failure

### Fixed

- Revert macro core changes, causes crashes when flowering sometimes
- Updater now waits for `SleepHunter.exe` instances to terminate before updating

## [4.2.0] - 2023-04-11

### Added

- Support for logging to files
- `LoggingEnabled` user setting to enable generating log files (off by default)
- Logging throughout the application
- Basic inversion of control (IoC) framework
- Dark Ages client titles now renamed to `Darkages - ${Character}` for identifying multiple instances
- Dark Ages client titles renamed back to `Darkages` when logged out of a character

### Changed

- User settings version is now `1.1`
- User settings version is now updated on save
- Initialize services on startup before app load
- Improved wording on several error dialogs
- Flower worker is now more responsive (100ms -> 16ms delay)
- Client versions (`Versions.xml`) are only saved when the existing file does not exist
- Color themes (`Themes.xml`) are only saved when the existing file does not exist

### Removed

- Annoying "unable to save file" popups on close, are logged instead
- More dead code

### Fixed

- Flowering target should now wait for `If Mana < X` thresholds for alts
- Flowering targets should better multiple handle alts (not getting stuck queue)
- Updater throwing error when `Settings.xml` already exists
- Updater should use same color theme as main application

## [4.1.0] - 2023-04-10

### Added

- New `Updates` section in `Settings` window allowing version checking
- New `Auto-Update` window for downloading updates and launching the updater
- New `SleepHunter.Updater` child project for applying auto-updates
- New scanline overlay for displaying modals
- Check for new updates on startup (if enabled)

### Changed

- Accessor key for `About` is now `Alt+B` instead of `Alt+A` (conflicted with `All Macros`)

### Removed

- Dead code for MVVM (use `CommunityToolkit.Mvvm` instead)
- Old `Debug.WriteLine` calls (only used in Debug mode)

## [4.0.1] - 2023-04-09

### Added

- Proper CHANGELOG format

### Changed

- **Version is now 4.0.1** to align with the namesake
- .NET Framework 4.8.1 support
- Updated personal email address

### Removed

- .NET Framework version display in `Settings->About`

## [1.5.0] - 2016-09-13

### Added

- Support for DA Client 7.41
- Main toolbar buttons now recognize ALT shortcut key
- Drag and drop support for spell queue list
- Drag and drop support for flowering queue list
- Improved tool tip for undefined spell warning
- Better support for 64-bit Win32 APIs
- More color themes based on Google Material palette

### Changed

- Default window size increased to 1024x768
- Minimum window size increased to 800x600
- Main toolbar icons now use Segoe UI Symbol font
- Main toolbar text labels slightly modified
- Use Segoe UI Symbol font for icons
- Reduced font sizes in many areas
- New selection indicator that shows a left bar instead of full cell highlight
- Character list font sizes have been decreased
- Character list health and mana bars are slightly taller
- Skills tab is no longer scrollable, uses compact sub-tabs for Temuair/Medenia/World
- Spells tab is no longer scrollable, uses compact sub-tabs for Temuair/Medenia/World
- Spell queue is now always visible when not empty
- Spell queue no longer hidden when switching to other tabs
- Spell queue now has "remove" and "clear all" buttons for clarity
- Spell queue warning indicator now flashes
- Spell queue rotation now defaults to singular order instead of round robin (can be changed)
- Flowering queue list item layout redesign for clarity

### Removed

- Spell queue "move up"/"move down" buttons (in favor of drag and drop)
- Flower queue "move up"/"move down" buttons (in favor of drag and drop)

### Fixed

- UI threading issues that could cause `InvalidOperation` exceptions
