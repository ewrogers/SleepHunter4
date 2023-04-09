# Changelog
All notable changes to this library will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.1] - Unreleased

### Added

- Proper CHANGELOG format

### Changed

- .NET Framework 4.8.1 support

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
