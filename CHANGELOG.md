# Changelog

Notable changes to SleepHunter are listed here for people who use the application.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Added a new per-client automation system that now powers spell, flower, and skill macros with more predictable execution.
- Expanded support for live 7.41 client information, including character details, group members, active effects, dialogs, nearby entities, inventory, equipment, skills, and spells.
- Added a native-style vertical cooldown overlay to skills so remaining cooldown time is easier to see.
- Added more complete live inventory names and stack information.

### Changed

- Refreshed accent styling throughout controls, tabs, queues, progress bars, and metadata headers.
- Made theme highlights automatically use light or dark contrast based on the selected accent color.
- Added subtle hover borders to dropdowns, numeric controls, and checkboxes, plus small pressed animations for dropdown and numeric-control arrows.
- Redesigned client HP and MP bars as taller, aligned 10-segment displays with fixed-width labels and values.
- Made the main and metadata windows resizable from every edge and corner.
- Updated the spell queue with a taller progress bar, clearer selected and active states, white unselected names, and centered guidance when the queue is empty.
- Simplified flower queue conditions to read as `WHEN [TIMER] > time OR [MP] < amount`, restored live countdowns, and reset displayed timers when automation stops.
- Combined Start Macro, Pause Macro, and Resume Macro into one state-aware toolbar button.
- Allowed skill toggles and spell or flower queue changes while automation is running.
- Allowed ready skills to run during spell casting because skill input does not interrupt the active cast.
- Improved automation behavior while typing, when dialogs open, when maps or coordinates change, and when the client temporarily switches panels.
- Improved automatic staff selection so it respects class and level requirements and only switches when casting would benefit.
- Changed current macro saves and autosaves to the versioned `.sh4x` format. Older `.sh4` files remain available for import.
- Added slot numbers to inventory, skill, and spell grids, plus abbreviated slot labels to equipment.
- Improved performance when opening large metadata lists.
- Replaced the small runtime indicator with a full-width status bar that shows `Healthy`, a concise error, or `No clients`, with selectable details when available.
- Renamed the default character sort from Login Time to Launch Order and continued sorting clients from oldest to newest.
- Improved global hotkey assignment and transfer between characters.
- Upgraded SleepHunter and the updater to .NET 10 LTS.
- Simplified client setup by replacing separate version profiles with one verified configuration for supported clients.
- Improved client launch and patch handling so failures are reported and partially launched clients are cleaned up safely.
- Unified the client list, vitals, inventory, equipment, skill, and spell displays behind the same coherent runtime snapshots used by automation.
- Made current `.sh4x` macro files reject ambiguous duplicate fields instead of silently accepting them.

### Removed

- Removed the Zolian-only Water & Beds feature.
- Removed legacy Zolian and Auto-Detect client profiles, along with the unused client-version selector.
- Removed the old background client-reading path that duplicated the current runtime and could allow displayed information to disagree.

### Fixed

- Fixed a runtime error that could occur after a flower cast completed and its next waiting interval began.
- Fixed normal spell casting and flowering failing to interleave when a prioritized flower target was temporarily unavailable.
- Prevented Fas Spiorad from being cast again before its successful mana update can be observed, while preserving retries when mana stays low.
- Kept Stop Macro available during capture errors and removed stale running indicators when a client runtime stops unexpectedly.
- Fixed automatic flowering retaining a removed flower action and stopping the runtime when normal spell casting resumed.
- Flower queue additions, edits, reordering, and removals now take effect safely while a macro is running.
- Fixed Fas Spiorad behavior so active casts are rechecked against current mana and cancelled when no longer needed, while still retrying when mana is low.
- Prevented automatic staff selection from unequipping a weapon unless another usable staff improves casting.
- Restored the active-cast highlight on the correct spell queue item.
- Fixed unexpected runtime failures appearing as a stale `Healthy` state and leaving macro controls unusable.
- Fixed several UI update issues that could leave toolbar commands, selected queues, or runtime details out of date.
- Fixed the runtime-details popup so it can be reopened and long diagnostics are not clipped.
- Corrected character class detection, including Wizard, for supported clients.
- Corrected the level requirement for early Priest and Wizard staves from 19 to 11.
- Corrected all Instrumental Attack ranks to use normal skill activation.
- Prevented stale client data from appearing as a garbled character name.
- Corrected equipment and appearance readings for the supported 7.41 client.
- Restored equipment icons and durability details, plus inventory durability numbers, for equipped and carried items.
- Fixed skill cooldowns remaining visible after they end and restored live spell cooldowns in the UI.
- Corrected inventory and equipment durability values, improved durability tooltip color, and made inventory tooltips respond across the full slot.
- Fixed clients starting in certain inns appearing logged out until they changed maps.
- Kept the last valid map visible during map transitions while preventing automation until the new location is read consistently.
- Kept the Start Macro label visible before selecting a client and preserved Resume Macro for paused clients.
- Restored updates to the selected macro queue.
- Corrected skill and spell book handling so all 90 slots are supported and unused slots are cleared.
- Cleared stale inventory, skill, spell, and chat information when its live source disappears.
- Improved dialog cleanup so Escape is sent only when a popup is actually visible and stacked dialogs close one at a time.
- Restored reliable selected-character hotkey assignment and glyph display, global start, pause, and resume control, immediate autosave and startup restore, plus `Escape` clearing.
- Kept client updates running when optional spell information cannot be read, allowing HP and MP displays to recover after revival or flowering.
- Fixed missing client executables being reported incorrectly and ensured failed suspended launches are terminated safely.

## [4.11.2] - 2026-07-24

### Added

- Added an Improved Auto-Follow patch, enabled by default, that follows players and monsters without attacking when Shift is held during right-click. The minimum follow distance can be set from 1 to 10 tiles.

## [4.11.1] - 2026-07-18

### Added

- Added separate options for a draggable exchange window and exchange results in the floating message bar. The draggable window is enabled by default, while floating results are disabled by default.
- Added a dedicated Patches section in Settings with Startup, Input, Render, and Interface tabs.

## [4.11.0] - 2026-07-18

### Added

- Added an enabled-by-default option that shows stack quantities in merchant and storage dialogs. Long item names are shortened to keep quantities visible.
- Added inventory item icons using the client's sprite, palette, and dye assets.
- Added an icon-based equipment tab that matches the client pane and includes item, sprite, slot, and durability tooltips.
- Added an enabled-by-default patch that shows up to 255 ground items as translucent hints while either Alt key is held.

### Fixed

- Improved inventory reliability and display, including item names, stack quantities, icons, gold formatting, durability tooltips, and coherent updates while the client changes inventory data.

## [4.10.4] - 2026-07-17

### Added

- Added an enabled-by-default option that suppresses the login notification and transfer delay for supported clients.
- Added an enabled-by-default option that clears held keys and modifiers when the Dark Ages client loses focus.

### Fixed

- Included required client-version and runtime data files in Debug and published builds.
- Fixed runtime data loading when SleepHunter is started from a different working directory.

### Security

- Removed a dependency that introduced known vulnerable packages.

## [4.10.3] - 2025-10-26

### Changed

- Updated SleepHunter to .NET 9.
- Improved panel switching to reduce misplaced clicks.
- Improved macro cleanup when a client exits.

### Fixed

- Fixed macro autosave and autoload.
- Fixed the spell queue not being included correctly in autosaves.

## [4.10.2] - 2023-06-29

### Changed

- Improved general application performance and removed unused legacy behavior.

### Fixed

- Improved autosave and autoload reliability.
- Fixed several interface issues that could occur after an automatic load.

## [4.10.1] - 2023-06-27

### Added

- Added configurable minimum and maximum HP conditions for abilities such as Crasher, Animal Feast, and Execute.
- Added a spell queue indicator for abilities waiting on an HP condition.
- Added support for marking spells that open dialogs.
- Added HP conditions to ability tooltips and expanded metadata editor tooltips.

### Changed

- Updated skill and spell metadata editors with larger dialogs and clearer layouts.
- Updated ability behavior to use configurable HP conditions instead of hard-coded skill names.

### Fixed

- Improved automatic dialog closing.
- Improved autosave error messages.
- Added thousands separators to HP and MP values.

## [4.10.0] - 2023-06-27

### Added

- Added Save Macro and Load Macro toolbar buttons.
- Added a configurable window title for client versions.
- Added macro feature settings to saved macro files.

### Changed

- Renamed Start New Client to Launch Client.
- Increased the minimum window width for the new toolbar buttons.
- Improved macro and per-character feature storage.

### Fixed

- Improved autosave reliability.
- Fixed the spell rotation dropdown occasionally appearing empty.

## [4.9.0] - 2023-06-24

### Added

- Added an Inventory and Equipment toggle to the Items tab.
- Added an equipment list organized by slot.

### Changed

- Renamed the Inventory tab to Items.
- Added vertical scrolling to Inventory, Skills, and Spells when needed.

### Fixed

- Corrected Zolian equipment readings so automatic staff switching works.

## [4.8.2] - 2023-06-23

### Added

- Added the Zolian-only Water & Beds MP recovery option.
- Added a Browse button for selecting the client executable.

### Changed

- Limited the Flowering tab to USDA clients.
- Improved the color-theme dropdown layout.
- Automatically opened the spell queue when selecting a character with queued spells.

## [4.8.1] - 2023-06-22

### Added

- Added gold to the final inventory slot.
- Added a Features tab for per-character client options.

### Changed

- Renamed client version 7.41 to USDA 7.41 for clarity.

### Fixed

- Fixed Fas Spiorad casting unnecessarily at low mana.
- Improved spell cooldown updates.

## [4.8.0] - 2023-06-21

### Added

- Added an Inventory tab that initially displays item names.
- Added inventory grid options under User Interface settings.
- Added support for detecting and managing additional client variants.
- Added a Zolian 9.1.1 client profile.

### Changed

- Improved client detection when launching or attaching to clients.
- Increased the threshold for abbreviated HP and MP values to 10,000.
- Enlarged the User Settings window.

### Fixed

- Fixed skill and spell names that do not include level text.

## [4.7.0] - 2023-06-16

### Added

- Added per-character spell queue rotation.
- Added spell cooldown indicators to the spell queue.
- Added a Skip Spells on Cooldown option, enabled by default.
- Added more keyboard shortcuts to Spell Macro settings.

### Changed

- Renamed Spell Rotation Mode to Default Spell Queue Rotation.
- Formatted large HP and MP values with `k` and `m` suffixes, such as `256k` and `1.2m`.

### Fixed

- Corrected several staff casting-line values.
- Improved spell queue rotation behavior.

## [4.6.1] - 2023-06-03

### Added

- Added running and paused status icons beside character names.

### Changed

- Improved spacing and sizing in the character list.
- Added more locations to check for the default Dark Ages client.

### Removed

- Removed the DirectDraw Compatibility Fix because it caused side effects for some users.
- Users who enabled that fix should delete `ddraw.dll` and `DDrawCompat-Darkages.ini` from the Dark Ages client folder.

### Fixed

- Marked Assail, Assault, and Clobber as assail skills.
- Cleared macro status when stopping, preventing stale states such as `Assailing`.
- Updated Execute to wait until the target is below 2% HP.
- Fixed spell queue levels not updating until the next cast.

## [4.6.0] - 2023-05-31

### Added

- Added an optional DirectDraw Compatibility Fix for flickering mouse cursors.

## [4.5.5] - 2023-05-21

### Added

- Added many missing Temuair and Medenia spells and skills, including abilities above AB 50.

### Fixed

- Corrected casting lines and mana costs for several spells.
- Improved keyboard navigation in several parts of the interface.

## [4.5.4] - 2023-05-19

### Fixed

- Improved skill cooldown detection.

## [4.5.3] - 2023-05-18

### Fixed

- Fixed memory scanning on 64-bit systems to make cooldowns more reliable.
- Reset cooldown tracking when logging back into the same client instance.

## [4.5.2] - 2023-05-18

### Added

- Added missing high-level Medenia staves for all classes, including Bard and Summoner staves above AB 70.

### Fixed

- Fixed cooldown detection on 64-bit systems.
- Improved macro toolbar updates after map or location changes.
- Hid the spell queue after the final character logs out.
- Added support for display scaling such as 150% and 175%.

## [4.5.1] - 2023-04-18

### Added

- Added Escape-key support for closing Spell Target and Flower Target dialogs.

### Fixed

- Hid No Target for spells that require a target.
- Hid mouse offsets when they do not apply to the selected spell or flower target.

## [4.5.0] - 2023-04-18

### Added

- Added CPU and .NET version information to Settings > About.

### Changed

- Moved SleepHunter from .NET Framework 4.8.1 to .NET 7.
- Removed the build date from Settings > About.

## [4.4.2] - 2023-04-18

### Changed

- Required the client-version file instead of silently resetting or overwriting it.
- Stopped overwriting theme and metadata files when SleepHunter closes.
- Added a safe fallback to the default theme when a theme cannot be loaded.
- Replaced the old status bar with a thinner border and resize grip.

### Fixed

- Fixed character sorting by login time.
- Fixed Show All Processes not being applied correctly at startup.

## [4.4.1] - 2023-04-16

### Added

- Added Show All Processes under Settings > Debug.
- Added client launch times and a configurable character sort order.

### Changed

- Hid clients that are not logged in by default. They can still be shown with Show All Processes.
- Sorted clients from oldest to newest by default.
- Improved metadata editor layouts.

### Fixed

- Disabled spell queue actions when the queue is empty or no client is selected.
- Disabled skill and spell tabs when no client is selected.
- Fixed the SleepHunter window title occasionally failing to update.

## [4.4.0] - 2023-04-15

### Added

- Added a Debug tab to Settings.
- Added more skill and spell metadata.
- Added spell mana costs to the Spell Target dialog.
- Added animated resizing to Spell Target and Flower Target dialogs.
- Added links to the user manual and an optional first-run prompt to open it.
- Added a spell queue toggle button and a Stop All toolbar button.

### Changed

- Added a new collection of color themes.
- Improved Settings layouts, keyboard shortcuts, and wording.
- Moved the Metadata Editor to the main toolbar.
- Redesigned Spell Target and Flower Target dialogs.
- Added decorated numeric inputs for units and contextual labels.
- Changed flower queue items to show timers.
- Renamed Tile Radius to Tile Area and Absolute X/Y to Screen Position.
- Limited screen-position targeting to coordinates from 0 through 1280x960.
- Improved dropdown sizing, tooltip timing, and spell queue presentation.

### Removed

- Removed Relative Coordinates targeting because Self with X/Y offsets provides the same behavior.
- Removed Rainbow Mode.
- Removed Reset Themes and Reset Version buttons.
- Removed the missing-spell warning and flower queue progress bar.
- Removed the old `Ready` status-bar text.

### Fixed

- Disabled macro controls until a client is logged in.
- Improved numeric value entry, validation, and lost-focus updates.
- Improved selected spell queue text contrast.
- Limited double-click actions to the left mouse button.
- Selected the default theme when saved theme settings are invalid.
- Opened the spell queue only when adding a spell, not when changing tabs.

## [4.3.0] - 2023-04-11

### Added

- Added a standalone updater that can update itself before installing a new SleepHunter release.
- Added a visual divider between vertical Settings tabs.

### Changed

- Reduced the main window title and general interface font sizes.
- Increased HP and MP text slightly.
- Widened common dropdowns and text inputs.
- Improved interface contrast and spacing.

### Fixed

- Improved handling when the configured client path points to missing files.
- Added a focus border to numeric controls.

## [4.2.1] - 2023-04-11

### Added

- Added a Retry button to the updater.

### Fixed

- Reverted a macro change that could cause flowering crashes.
- Made the updater wait for SleepHunter to close before installing an update.

## [4.2.0] - 2023-04-11

### Added

- Added optional file logging.
- Renamed Dark Ages client windows to include the character name while logged in.

### Changed

- Improved wording in several error messages.
- Made flowering checks more responsive.
- Stopped overwriting existing client-version and theme files.

### Removed

- Removed repeated save-error popups during shutdown. Errors are written to the log instead.

### Fixed

- Fixed flower targets not waiting for their configured mana threshold.
- Improved flower queue behavior with multiple alternate characters.
- Fixed updater errors when `Settings.xml` already exists.
- Made the updater use the same color theme as SleepHunter.

## [4.1.0] - 2023-04-10

### Added

- Added update settings, automatic update downloads, and a dedicated updater.
- Added optional update checks at startup.
- Added a scanline overlay behind dialogs.

### Changed

- Changed the About keyboard shortcut to Alt+B to avoid conflicting with All Macros.

## [4.0.1] - 2023-04-09

### Added

- Added a structured changelog.

### Changed

- Updated the version to 4.0.1.
- Added .NET Framework 4.8.1 support.

## [1.5.0] - 2016-09-13

### Added

- Added support for Dark Ages client 7.41.
- Added Alt-key shortcuts to the main toolbar.
- Added drag-and-drop reordering to spell and flower queues.
- Added clearer warnings for spells with missing definitions.
- Added more Material-inspired color themes.

### Changed

- Increased the default window size to 1024x768 and the minimum size to 800x600.
- Updated toolbar icons and labels.
- Reduced font sizes in several areas and made client HP and MP bars taller.
- Changed list selection to use a narrow bar instead of a full-row highlight.
- Reduced character-list text size.
- Reorganized Skills and Spells into compact Temuair, Medenia, and World tabs.
- Kept the spell queue visible whenever it contains items.
- Added Remove and Clear All controls to the spell queue.
- Changed the default spell queue rotation from Round Robin to Singular Order.
- Redesigned flower queue items for clarity.

### Removed

- Removed Move Up and Move Down queue buttons in favor of drag-and-drop reordering.

### Fixed

- Fixed interface threading problems that could cause `InvalidOperation` errors.
