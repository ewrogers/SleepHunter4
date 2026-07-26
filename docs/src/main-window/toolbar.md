# Toolbar

![image](../screenshots/tool-bar.png)

The toolbar has the following buttons:

- [Launch Client](#launch-client)
- [Load State](#load-state)
- [Save State](#save-state)
- [Start, Pause, and Resume Macro](#start-pause-and-resume-macro)
- [Stop Macro](#stop-macro)
- [Stop All](#stop-all)
- [Metadata Editor](#metadata-editor)
- [App Settings](#app-settings).

## Launch Client

This button will launch a new game client, applying any tweaks that are enabled in the [Settings](./settings.md#game-client) window.

By default, game clients that are not actively "logged in" will **not** be displayed in the list.

## Load State

This button will open a dialog to load the current character's macro configuration.
Current `.sh4x` JSON files and legacy `.sh4` XML files can be loaded.

**NOTE:** Skills and spells that are not currently available on the character are preserved in the configuration instead of being discarded.

## Save State

This button will save the current character's macro configuration as a versioned `.sh4x` JSON file.
Legacy `.sh4` XML is import-only and is never written by the new save path.

## Start, Pause, and Resume Macro

This state-aware button starts macroing the selected character and changes to
`Pause Macro` while automation is running. After pausing, the same control
changes to `Resume Macro`. It shows the pause icon only while automation is
running and uses the play icon for starting or resuming. Pausing retains the
current macro state.

## Stop Macro

This button will stop macroing the selected character, resetting the macro state.

## Stop All

This button will stop macroing on all characters, resetting the macro state.
It is equivalent to clicking the `Stop Macro` button for each character.

### Pause vs Stop Macro

The main difference between `Pause Macro` and `Stop Macro` is that pause acts as a temporary stop, while stop will reset the macro state for that character.
You can notice this with certain timers like flower targets when you pause/resume versus stopping and re-starting.

**NOTE:** This does **not** mean that your skills and spells will be removed from the queue, only that the macro state will be reset.

## Metadata Editor

This button will open the [Metadata Editor](./metadata-editor.md) window.

## App Settings

This button will open the [Settings](../settings/general.md) window.
