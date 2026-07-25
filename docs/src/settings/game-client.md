# Game Client Settings

The `Game Client` settings tab contains settings for the Dark Ages game client.

## Client Path

This setting determines the path to the Dark Ages game client executable.
It is also used to determine the path to the Dark Ages game client data files for rendering icons.

## Client Layout

SleepHunter uses one client layout from `Versions.xml` for memory addresses,
process detection, and runtime patch metadata. `Auto-Detect` verifies the
configured client signature but does not select among alternate memory maps.

Compatible custom clients can use updated addresses and patch metadata in the
same mapping without code changes.

Client launch modifications are organized separately under [Patches](./patches.md).
