# Changelog
All notable changes to this library will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.4.1] - Unreleased

### Fixed

- Disable spell queue remove buttons on startup (when empty)
- SleepHunter window title not changing sometimes

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
