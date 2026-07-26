# Changelog
All notable changes to this library will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Added disabled-by-default runtime automation configuration and deterministic observation-driven cycles for spells, flowering, and skills
- Added typed numeric memory variables for byte, signed-byte, 16-bit, and 32-bit values while preserving legacy formatted-string mappings for older client profiles
- Expanded supported-client character state with base class, advanced display class, level, ability level, character ID, user state, action lock, progression, attributes, vitals, weight, combat modifiers, elements, nation, title, guild, guild rank, and self-look metadata
- Added the parsed 64-entry group-member cache, including names and starred state
- Added immutable active spell-effect snapshots with icon and server-supplied duration stage
- Added immutable message-dialog snapshots with registration metadata, copied display text, and a generic `IsPopupOpen` projection
- Added bounded world-entity snapshots for ground items, monsters, NPCs, and players, including coordinates, documented sprites, RTTI type, creature state, human appearance, resolved resource identities, and monster-disguise state
- Added pane-backed 90-slot skill and spell state, including action delays, learned-level suffix data, spell cast lines, and skill cooldown progress and wrap-safe timestamps
- Added a native-style 30-step vertical skill cooldown overlay that shrinks from top to bottom as the client progress counter advances
- Added inventory pane display names and stackability alongside the existing stable compact inventory identity
- Added focused tests for pointer walking, typed values, memory mappings, pane layouts, character classes and names, inventory and equipment snapshots, and cooldown wraparound

### Changed

- Renamed the WPF project to `SleepHunter.App` and moved the application, updater, and application tests under `src/` and `tests/` while preserving their product names, namespaces, and executable names
- Reconciled the supported client memory roots and offsets with the documented `WorldPane`, `WorldUserFunc`, `GUIBackPane`, `EquipPane`, and event-dispatcher layouts
- Collapsed Runtime and Interop onto one configurable client layout, removed version routing from client identities, and renamed client parsers, capture, and input planning types to generic names
- Kept compact inventory, skillbook, spellbook, and legacy cooldown paths as compatibility fallbacks when richer pane snapshots are unavailable or change during a read
- Made automatic staff selection enforce class and normal or ability-level requirements, prefer the highest progression staff among equal casting improvements, and avoid switching when casting lines would not improve
- Updated the test SDK, NUnit framework, NUnit analyzers, and NUnit adapter to their latest .NET 10-compatible stable releases
- Upgraded the application, updater, runtime, interop, persistence, and test projects to .NET 10 LTS
- Changed chat typing detection to use the globally focused, visible, live chat or tell input pane and renamed the snapshot projection to `IsChatOpen`
- Added generation checks, count validation, bounded traversal, and coherent snapshots around mutable client-owned pointers and collections
- Moved client cards to an MVVM projection that uses coherent runtime name, presence, map, health, and mana observations when available, with automatic legacy fallback after missing or failed captures
- Projected immutable automation configuration and enabled state through the Community Toolkit runtime ViewModel
- Promoted per-client runtime hosts from read-only shadow capture to the active command boundary, with a shared clock, immutable ability metadata, and deduplicated cross-client rosters
- Made deterministic automation wait while the user is typing, dismiss dialogs opened by spells as well as skills, and apply configurable pause or stop policies when the observed map or coordinates change
- Changed popup cleanup to react to coherent dialog observations, re-observe after each Escape, and close stacked popups one at a time
- Preserved the user-selected client panel across automatic spell, skill, and flowering actions through deterministic, bounded restoration attempts
- Added an application composition boundary that converts persisted queues and macro settings into an atomic runtime setup command with class-aware staff catalogs
- Honored the option to wait behind a cooling spell instead of skipping it, and safely mapped legacy close-client movement actions to runtime stop behavior
- Routed toolbar, hotkey, and stop-all lifecycle controls through Community Toolkit commands backed by the deterministic runtime
- Adapted the current macro editor configuration through the tested legacy migration path before each start or resume
- Replaced the legacy executable macro state with a DI-owned Community Toolkit observable editor configuration, and projected queued spell levels and readiness from immutable runtime snapshots
- Changed current macro saves and autosaves to bounded, versioned `.sh4x` JSON while retaining XML `.sh4` files as import-only legacy configurations
- Labeled `.sh4x` as SleepHunter 4 Macro Files and `.sh4` as SleepHunter 4 Legacy Files in macro file dialogs
- Added dim slot numbers to inventory, skill, and spell grids, plus abbreviated top-left slot badges to the equipment grid
- Restored recycled row virtualization in the metadata editor and shared list styles so opening large skill, spell, and staff collections does not construct every row up front
- Moved the shared checkbox tick down to center it within its checkbox content area
- Replaced the client-card runtime letter tooltip with a full-width status bar that shows `Healthy` or a concise red error on the left and opens selectable diagnostics from the right
- Moved rolling average, minimum, and maximum snapshot-read times from the status indicator into runtime details and removed the ticking snapshot sequence
- Changed the combined macro toolbar button from the play icon to the pause icon while its selected macro is running
- Combined Start Macro and Pause Macro into one state-aware toolbar control that also becomes Resume Macro while paused
- Allowed ready skills to run during an active spell cast because skill input does not interrupt casting
- Allowed skill toggles and spell or flower queue additions, edits, removals, clearing, and reordering while automation is running, applying each complete setup atomically through the runtime command channel
- Moved spell and flower queue selection, removal, clearing, rotation, and flowering options into observable bindings and Community Toolkit commands
- Moved macro load, save, autosave, legacy autosave migration, and spell-queue visibility into tested application services and Community Toolkit commands
- Replaced the window-owned process and client `BackgroundWorker` loops with independently paced, cancellable async polling that is awaited during shutdown
- Moved global hotkey assignment, transfer, clearing, and rollback behavior into a tested application service
- Renamed the default Login Time character sort to Launch Order and based it on process creation timestamps from oldest to newest
- Replaced the version collection and selector with one bounded `ClientLayout.xml` document shared by application and Interop memory readers
- Corrected the unified `MapName` mapping to declare its final string-pointer indirection instead of relying on the legacy reader's conditional string heuristic
- Moved suspended client launch, patch planning, verification, failure cleanup, and resume behavior into a tested application service exposed through a Community Toolkit command

### Removed

- Removed the Zolian-only Water & Beds automation, its feature tab, client feature flag, and private macro-state storage
- Removed legacy Zolian and `Auto-Detect` mapping profiles, signature-based client version routing, and the unused client version selector
- Removed the legacy macro executor, lock-based queue processing, deferred dispatcher, execution flags on players, and the 16 ms flower update worker
- Removed the WPF-era macro XML serializer and serialized state DTOs from the current save and runtime-start paths
- Removed the transition-era shadow macro configuration view model and duplicate queue synchronization on file load
- Removed the unused blocking player-interface input stack, direct `PostMessage` automator, deferred-action residue, panel-coordinate helpers, and other unreferenced legacy utilities
- Removed the Sense-specific and single-pointer dialog-open mappings in favor of the active event-dispatcher collection
- Removed the old chat byte flag and broad event-tree chat scan in favor of the authoritative focused `InputMan` pane
- Removed unused application helpers, events, converters, collection APIs, and the unconsumed world-entity model that duplicated live client memory traversal

### Fixed

- Marshaled legacy client observation notifications through the WPF dispatcher so command availability updates cannot access bound buttons from the client-polling thread
- Reset the runtime-details toggle when its popup is dismissed so the details panel can be reopened immediately
- Stretched the runtime-details text viewport so multiline error diagnostics and scrollbars are not clipped at the bottom
- Corrected runtime character-class decoding to use the client's sequential values from `0` for Peasant through `5` for Monk, including `3` for Wizard
- Corrected the early priest and wizard staff requirements from level 19 to level 11
- Corrected every Instrumental Attack rank to use normal skill-slot activation instead of assail input
- Prevented stale or unallocated supported-client character-name buffer contents from appearing as a gibberish player name by requiring a live session generation, a bounded NUL-terminated read, and a structurally valid name
- Kept the executable-verified `EquipPane` singleton at `0x006FC914` for equipment and self-look profile fields; the nearby `0x006FC8EC` global documented by the newer reference is null in the signed `7D4E--1K` client
- Clear pane-backed skill cooldown state when the client's `cooldown_visual_active` flag clears instead of treating the retained nonzero progress counter as an active cooldown
- Show pane-backed spell cooldowns in the UI from the client's live action-delay state
- Corrected live inventory and equipment pane durability ordering to read maximum durability before current durability, and changed durability tooltip text to a cooler blue
- Held map transitions until map number and name form a coherent identity, while retaining the last coherent UI projection and continuing to reject automation actions until capture recovers
- Made occupied inventory tooltips respond across the full slot instead of only over the item sprite
- Kept the Start Macro caption visible before a client is selected while preserving the Resume Macro caption for paused clients
- Restored selected macro queue notifications by correcting the reversed subscription guard
- Corrected compact skill and spell counts to 89 while supporting the pane model's 90th slot, clearing unused tail slots, and including the last slot in each book-panel view
- Reset stale pane-only item, skill, spell, and chat state when the corresponding live data is no longer available
- Avoid sending Escape when an expected popup never appears, while immediately dismissing an observed popup without waiting for the old fixed delay
- Corrected global hotkey reassignment to release the active registration instead of an unregistered replacement value, while retaining the previous assignment when a native operation fails
- Restored selected-character hotkey capture through preview input and made each registered global hotkey toggle its owning character without relying on the active window or current selection
- Kept per-client capture observations flowing when optional spell-state projection fails, allowing health and mana displays to recover after zero-health revival and zero-mana flowering observations
- Report a missing client executable as a launch failure and terminate suspended clients when patching or thread resume fails

## [4.11.2] - 2026-07-24

### Added

- Added an enabled-by-default Improved Auto-Follow input patch for following players and monsters without attacking by holding Shift while right-clicking, with a configurable minimum distance from 1 to 10 tiles

## [4.11.1] - 2026-07-18

### Added

- Added independent client options for a draggable exchange window (enabled by default) and exchange results in the floating message bar (disabled by default)
- Added a dedicated Patches settings section with Startup, Input, Render, and Interface tabs for organizing client launch patches

## [4.11.0] - 2026-07-18

### Added

- Added an enabled-by-default client option that displays stack quantities greater than one in inventory-based merchant and storage dialogs, truncating long names with two dots to keep the quantity visible
- Added inventory item icon rendering from client sprite, palette, and dye assets
- Added a dedicated icon-based equipment tab matching the client pane layout, with item, sprite, slot, and durability tooltips
- Added an enabled-by-default client patch that reveals up to 255 ground items as translucent hints while either Alt key is held

### Fixed

- Decode compact inventory sprites before rendering, read inventory records as coherent snapshots, preserve digits in item names, display live stack quantities and centered native-size icons without under-slot labels, add sprite/quantity/durability tooltips, and show gold amounts with thousands separators without including the amount in the item name

## [4.10.4] - 2026-07-17

### Added

- Added a client option to suppress the login notification and transfer delay, enabled by default for supported clients

### Changed

- Removed the unused `Microsoft.Windows.Compatibility` metapackage from SleepHunter and the Updater
- Completed product, company, author, version, and informational-version metadata for the SleepHunter and Updater assemblies

### Fixed

- Added an enabled-by-default client launch option that clears pressed keys and modifiers when the Dark Ages client loses focus
- Included client versions and other runtime XML data in Debug and published builds
- Resolved runtime data files relative to the application directory regardless of the launch working directory

### Security

- Removed vulnerable transitive dependencies previously introduced by `Microsoft.Windows.Compatibility`

## [4.10.3] - 2025-10-26

### Changed

- Updated to .NET 9.0
- Fix panel switching to be more reliable timing (prevent misclicks)
- Fix autosave/autoload
- Spell queue now saves properly to autosave
- Better macro cleanup code on client exit

## [4.10.2] - 2023-06-29

### Removed

- Lots of old code and things that were not being used

### Fixed

- Auto-save and load reliability
- Some UI bugs on auto-load
- Lots of performance and under the hood optimizations

## [4.10.1] - 2023-06-27

### Added

- `MinHealthPercent` to ability metadata
- `MaxHealthPercent` to ability metadata (Crasher/Animal Feast/Execute)
- Visual indicator on spell queue when waiting on HP thresholds
- `OpensDialog` to spell metadata (was only skills before)
- HP threshold in ability tooltips
- More tooltips in metadata editors

### Changed

- Use new metadata properties for HP based conditions on abilities
- Redesign some metadata editors for skill/spells
- Increase dialog size for skill/spell metadata editors
- Tooltip design

### Removed

- Removed hard-coded names for HP-based skills (now is customizable)

### Fixed

- Improved dialog closing (deferred dispatcher)
- Autosave load error popup & info
- Thousands formatting for HP/MP

## [4.10.0] - 2023-06-27

### Added

- `Save Macro` toolbar button
- `Load Macro` toolbar button
- Window title parameter for client versions
- Macro features are now saved with state

### Changed

- Renamed `Start New Client` button to `Launch Client` to fit new `Load` and `Save` buttons
- Updated min width to fit new toolbar buttons
- Refactored entire save state system
- Refactored local storage (features)

### Fixed 

- Auto-save reliability
- Spell rotation combo box sometimes being empty

## [4.9.0] - 2023-06-24

### Added

- `Items` tab now features an `Inventory` / `Equipment` toggle for viewing each
- `Equipment` view to see all equipped items in a list (by slot)

### Changed

- `Inventory` tab has been renamed to `Items`
- `Inventory` will now scroll vertically (when necessary)
- `Skills` will now scroll vertically (when necessary)
- `Spells` will now scroll vertically (when necessary)

### Fixed

- Equipment memory offsets for Zolian (staff-switching now works)

## [4.8.2] - 2023-06-23

### Added

- "Use Water & Beds" for MP recovery feature (Zolian only)
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
