# SleepHunter Refactor Smoke Test

This checklist is the final manual validation gate for the .NET 10 runtime
refactor. Run it from a Release publish built from `codex/runtime-refactor`.
Automated tests validate deterministic engine decisions, snapshot parsing,
input planning, persistence, and application services. This pass validates the
remaining Windows, WPF, and live-client integration.

## Test Record

Record the environment before testing:

| Field | Value |
| --- | --- |
| SleepHunter commit | |
| Windows version | |
| Dark Ages executable | |
| Executable SHA-256 | |
| `ClientLayout.xml` commit or checksum | |
| Test characters and base classes | |
| Result | |

The launch patches currently require the verified 3,112,960-byte executable
with SHA-256
`054A5D6ADC56099C6BFD9D2A58675AFF62DC788B63209A3D906492F5B89E96C6`.
Memory capture uses the unified `ClientLayout.xml` shipped beside the
application.

## 1. Cold Start and Shell

- [ ] Start the published `SleepHunter.exe` with no game client running.
- [ ] Confirm the main window opens without an exception dialog.
- [ ] Confirm Launch Client is enabled after `ClientLayout.xml` loads.
- [ ] Confirm Start, Pause, Stop, and Stop All are disabled with no selected
  in-world client.
- [ ] Confirm the disabled Start Macro button still shows its caption before a
  client is selected.
- [ ] Open Settings and each metadata editor, then close them normally.
- [ ] Change the theme and each icon-grid width, then confirm the main view
  updates.
- [ ] Check the session log for startup exceptions and debugger output for WPF
  binding errors.

## 2. Client Launch and Patches

- [ ] Launch the verified client with every startup patch disabled.
- [ ] Confirm the suspended process resumes and reaches the login screen.
- [ ] Enable each desired patch and launch a second client.
- [ ] Confirm multiple-instance, intro, input, render, exchange, and dialog
  behavior matches the selected settings.
- [ ] Point a temporary test setting at a missing executable and confirm
  SleepHunter reports the failure.
- [ ] If an intentionally unsupported executable is available, confirm
  SleepHunter rejects it and leaves no suspended client process behind.

Do not test a deliberately corrupted patch against an important client
installation. Use a disposable copy.

## 2a. Metadata Editor

- [ ] Open the metadata editor and confirm the initial skills tab appears
  promptly without a long UI-thread pause.
- [ ] Scroll quickly through skills and spells, switch to staves, and confirm
  rows render correctly without blank, duplicated, or stale recycled content.
- [ ] Close and reopen the editor, then confirm add, edit, remove, save, and
  revert behavior still updates the visible collections.

## 3. Discovery, Attach, and Snapshots

- [ ] Confirm each launched process appears once in the client list.
- [ ] Select a client and confirm the compact bottom-right runtime indicator
  changes from waiting to `Healthy` without displaying timings or a ticking
  snapshot sequence.
- [ ] Trigger a recoverable capture failure and confirm the compact indicator
  shows the concise failure in red.
- [ ] Open the runtime details button, confirm nested mapping and memory failures
  include the variable, address, byte counts, native error code, and rolling
  average, minimum, and maximum capture times, then select and copy the
  diagnostic text with `Ctrl+C`.
- [ ] Dismiss runtime details by clicking outside it, reopen it immediately,
  close it with the details button, and confirm the text and scrollbars remain
  visible through the bottom of a multiline error.
- [ ] Log in and confirm name, class, map, coordinates, health, and mana update.
- [ ] Change maps and confirm the old map name and coordinates remain together
  until the new map name and coordinates appear together, without a persistent
  `MappingReadFailed` status for `MapName`.
- [ ] Confirm inventory, equipment, skill, and spell panes populate correctly.
- [ ] Confirm item quantities, durability, learned levels, and cooldown overlays
  update without stale entries.
- [ ] Leave a Debug build attached to a live client while observations update
  and confirm no cross-thread `NotifyCanExecuteChanged` exception occurs.
- [ ] Confirm inventory, skill, and spell slots show dim slot numbers, and
  equipment slots show the expected abbreviated top-left badges.
- [ ] Confirm current and maximum durability are in the correct order for both
  inventory and equipment, with cool blue durability text.
- [ ] Confirm an occupied inventory slot shows its tooltip from the full slot
  surface, including transparent space around the sprite.
- [ ] Open and close chat, dialogs, sense, inventory expansion, and minimized
  mode, then confirm their observed state clears correctly.
- [ ] Log out and back in without restarting SleepHunter.
- [ ] Close one of several clients and confirm only its card and runtime are
  removed.

## 4. Runtime Lifecycle

Use a harmless queue in a safe map for the first pass.

- [ ] Select a logged-in client and confirm Start becomes available after the
  first healthy in-world snapshot.
- [ ] Start automation and confirm the card shows a running state.
- [ ] Confirm the same toolbar control changes from Start Macro to Pause Macro
  and changes from the play icon to the pause icon while running, then returns
  to Resume Macro and the play icon while paused.
- [ ] Stop from the toolbar and confirm no further actions are issued.
- [ ] Run two clients, then use Stop All.
- [ ] Assign, transfer, invoke, and clear a global hotkey.
- [ ] Log out while automation is running and confirm the runtime stops with no
  later input.
- [ ] Close SleepHunter while automation is running and confirm shutdown
  completes without a hang or orphaned SleepHunter process.

## 5. Skills, Spells, Staffs, and Panels

- [ ] Double-click skills to enable and disable them while automation is
  running, and confirm automation remains running and uses the updated
  selection.
- [ ] Run a skill cycle and confirm cooldowns prevent premature reuse.
- [ ] While automation is running, add, edit, reorder, remove, and clear spell
  queue entries, and confirm each completed edit takes effect without pausing
  or stopping the macro.
- [ ] Test no-target, self, alternate-character, relative-tile, and
  screen-position targets that are safe for the test map.
- [ ] Confirm configured user-typing behavior defers automation while chat is
  active.
- [ ] Confirm spell and skill dialogs are dismissed only when configured.
- [ ] Confirm the previously selected client panel is restored after an
  automated action.
- [ ] Test a spell that requires a staff switch, then confirm the engine waits
  for the equipment snapshot before casting.
- [ ] Put an incompatible-class staff and a valid staff in inventory, then
  confirm automatic selection never chooses the incompatible staff.
- [ ] Confirm a missing compatible staff produces a bounded wait or skip and
  does not stall the event loop.

## 6. Flowering and Cross-Client State

- [ ] While automation is running, add, edit, reorder, remove, and clear flower
  targets, and confirm each completed edit takes effect without pausing or
  stopping the macro.
- [ ] Run flowering with an alternate character and confirm roster targeting
  follows live snapshots.
- [ ] Test Lyliac Plant and Lyliac Vineyard separately when the character
  supports them.
- [ ] Confirm flowering and spell automation respect their configured ordering.
- [ ] Remove or log out an alternate target and confirm automation does not act
  on stale roster data.

## 7. Policies and Deferred Actions

- [ ] Trigger the configured map-change pause or stop policy.
- [ ] Trigger the configured coordinate-change pause or stop policy.
- [ ] Confirm a pending cast, staff switch, panel transition, or dialog action
  is cancelled when automation stops or the client logs out.
- [ ] Confirm an action that is waiting on health, mana, cooldown, or a later
  snapshot does not busy-loop or block another client.
- [ ] Leave two clients running for at least ten minutes and confirm snapshot
  sequence numbers advance without duplicate actions or a growing UI delay.

## 8. Persistence

- [ ] Save a current macro and confirm the file extension is `.sh4x`.
- [ ] Confirm file dialogs label `.sh4x` as SleepHunter 4 Macro Files and
  `.sh4` as SleepHunter 4 Legacy Files.
- [ ] Confirm the saved `.sh4x` file is JSON and can be loaded again.
- [ ] Import a legacy `.sh4` XML macro, then save it and confirm the new output
  is `.sh4x` JSON.
- [ ] Restart SleepHunter and confirm autosaved queues, options, and hotkeys
  restore for the matching character.
- [ ] Confirm no current save path creates `.sh4`, `.shmacro`, or XML content.
- [ ] Confirm shipped `ClientLayout.xml`, `Skills.xml`, `Spells.xml`,
  `Staves.xml`, and `Themes.xml` continue to load.

## Pass Criteria

The refactor is ready for broader testing when:

- All critical items above pass on at least two character classes.
- No client is left suspended or partially patched after a launch failure.
- No automation input occurs after Stop, logout, client removal, or shutdown.
- There are no unhandled exceptions, persistent WPF binding errors, or
  continuously repeated log failures.
- `.sh4x` round trips and legacy `.sh4` import preserve the tested
  configuration.

Lua scripting is intentionally outside this refactor gate. It will build on the
runtime command, snapshot, and intent boundaries after this application pass is
stable.
