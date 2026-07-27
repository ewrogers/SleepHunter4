# SleepHunter Architecture

This document explains how SleepHunter observes a Dark Ages client, turns those
observations into deterministic macro decisions, issues guarded client input,
and presents the resulting state in the WPF application.

The most important design rule is that the automation engine never reads process
memory and never sends window input. It consumes immutable values and produces
immutable decisions. Windows-specific observation and input stay in the Interop
project.

## Project boundaries

```text
                         +--------------------------+
                         |    SleepHunter.App       |
                         | WPF, MVVM, configuration |
                         +-----+----------+---------+
                               |          |
                 +-------------+          +-------------+
                 |                                      |
                 v                                      v
 +---------------------------+             +---------------------------+
 | SleepHunter.Persistence   |             |  SleepHunter.Interop      |
 | .sh4x and legacy .sh4 I/O |             | memory capture and input  |
 +-------------+-------------+             +-------------+-------------+
               |                                         |
               +-------------------+---------------------+
                                   |
                                   v
                     +---------------------------+
                     |  SleepHunter.Runtime      |
                     | state, events, decisions, |
                     | intents, and automation   |
                     +---------------------------+
```

`SleepHunter.App` also references `SleepHunter.Runtime` directly for commands,
snapshots, configuration, and published runtime views.

The dependency direction is intentional:

- `SleepHunter.Runtime` contains deterministic domain types and has no WPF,
  Win32, or live-process dependency.
- `SleepHunter.Interop` depends on Runtime contracts. It translates live client
  memory into Runtime snapshots and Runtime intents into guarded window input.
- `SleepHunter.Persistence` stores versioned macro configuration without
  depending on WPF or live client access.
- `SleepHunter.App` composes the projects, owns user interaction, and projects
  runtime output into bindable presentation models.
- `SleepHunter.Updater` is a separate executable and is not part of the client
  runtime pipeline.

The client launcher patch flow is separate from automation. It writes only to a
newly launched, suspended process after checking exact signatures. Normal
runtime observation opens the game process with read-only memory access.

## One runtime per client

Each detected game process gets an independent `ClientRuntimeHost`.

```text
 Dark Ages process
        |
        | bounded read-only memory access
        v
 ClientSnapshotCapture
        |
        | SnapshotCaptureResult
        v
 ClientSnapshotScheduler
        |
        | latest capture observation
        v
 ClientRuntimeHost
        |
        +---------------------> App capture channel
        |                       ClientRuntimeViewModel
        |                       client list and detail panes
        |
        +---------------------> MacroSession
                                ClientSnapshotObserved event
```

`ClientRuntimeRegistry` in the App attaches and removes these hosts. It also
builds a shared character roster from the latest healthy client observations so
flower and spell targeting can resolve other logged-in characters.

`ClientDiscoveryCoordinator` polls only for new or closed game windows. It does
not refresh character state. Once a process is attached, the runtime snapshot
scheduler is the sole owner of client update cadence and observed game data.

## Snapshot capture

`data/ClientLayout.xml` describes the supported client memory layout.
`WindowsClientRuntimeFactory` loads that mapping and creates:

1. A read-only process memory source.
2. A `ClientSnapshotCapture`.
3. A `ClientSnapshotScheduler`.
4. An input planner and dispatcher.
5. A `ClientRuntimeHost` joining capture, runtime, and input.

The capture layer reads all requested sections into one immutable
`ClientSnapshot`:

- presence and character identity
- vitals and map location
- active panel, inventory expansion, and minimized interface state
- inventory and equipment
- skill and spell books, including cooldown observations
- group members and nearby world entities
- chat input and message dialog state
- active spell effects

The snapshot has a monotonically increasing sequence and capture timestamps.
The engine uses these values to distinguish a new observation from the snapshot
that existed before an action was issued.

### Coherence and failure behavior

The client can mutate memory while a capture is running. Capture code therefore
uses bounded reads, validates counts and pointers, and rejects incomplete or
incoherent results.

Map changes receive special treatment. A changed map identity must be observed
coherently before it replaces the accepted location. During the expected
transition:

```text
 old coherent map
        |
        v
 first changed or partial map observation
        |
        +----> LocationTransition result, no new ClientSnapshot
        |
        v
 repeated coherent changed map observation
        |
        +----> accepted ClientSnapshot for the new map
```

The App may continue displaying the last successful snapshot during this
specific transition. The engine receives only actual snapshots, so it never
treats retained presentation data as a new observation.

Every capture also records timing and memory-read metrics. Failures preserve a
typed section, failure reason, mapped variable, and nested memory error when
available. The runtime details UI displays this structure without starting a
second read path.

## Commands, events, state, and decisions

The Runtime project is an event-driven state machine.

```text
                    +----------------------+
 user or App ------>| MacroCommand         |
 snapshot --------->| MacroEvent           |
 action result ---->| scheduled deadline   |
 roster ----------->| internal event       |
                    +----------+-----------+
                               |
                               v
                    +----------------------+
                    | MacroEngine.Decide   |
                    | previous state       |
                    | + one event          |
                    | + current time       |
                    +----------+-----------+
                               |
                               v
                    +----------------------+
                    | MacroDecision        |
                    | next state           |
                    | raised events        |
                    | scheduled events     |
                    | optional intent      |
                    | optional view        |
                    +----------------------+
```

The terms have specific meanings:

- A **command** asks the runtime to change configuration or lifecycle state.
  Examples include start, pause, stop, replace queues, or request a spell cast.
- An **event** is one input to the engine. Commands are wrapped as events.
  Snapshots, client rosters, action issue results, automation-cycle requests,
  and elapsed deadlines are events too.
- `MacroState` is the immutable authoritative automation state for one client.
- A **decision** is the complete result of processing one event. It may contain
  a new state, immediate events, future scheduled events, one intent, and a
  published read-only view.
- An **intent** describes a client-side operation the engine wants performed.
  It does not contain Win32 calls.
- `MacroViewSnapshot` is the UI-safe projection of engine state.

`MacroDecisionInvariants` validates every decision before the session accepts
it. The checks enforce relationships such as:

- a client action intent must match the pending action identifier
- a pending action must have a future deadline
- panel, staff, skill, spell, and flower states must agree
- a changed state revision must publish the matching view revision

An invariant failure stops that client runtime instead of allowing contradictory
automation state to continue.

## The macro session event loop

`MacroSession` owns the engine and serializes all state changes through one
worker. Multiple producers can submit commands, snapshots, rosters, and action
results, but only the session worker calls `MacroEngine.Decide`.

```text
 wake
  |
  +-> drain commands, bounded per iteration
  +-> drain action issue results, bounded per iteration
  +-> take latest client snapshot
  +-> take latest shared roster
  +-> process every due scheduled event
  |
  +-> process immediate events raised by decisions
  |
  +-> publish intent and view outputs
  |
  `-> wait for input or the next deadline
```

Snapshots and rosters use latest-value mailboxes. If observations arrive faster
than the engine can consume them, stale intermediate values are replaced by the
newest value. Commands and action issue results use channels and remain ordered.
This keeps automation responsive without building an unbounded backlog of old
world observations.

Time-based behavior uses `MacroClock`, `MacroTimestamp`, and scheduled events.
The engine does not depend on wall-clock polling inside decision methods. Tests
can therefore use a manual `TimeProvider` and advance time deterministically.

## From intent to client input

Runtime intents remain platform-neutral until Interop receives them.

```text
 MacroDecision.Intent
        |
        v
 ClientRuntimeHost
        |
        v
 ClientIntentPlanner
        |
        | validates client and latest snapshot
        | resolves panel, slot, target, scale, and coordinates
        v
 ClientInputPlan
        |
        v
 WindowInputDispatcher
        |
        | verifies the target process and window
        | sends window messages
        v
 Dark Ages client
```

Examples of intents include:

- switch the active panel
- expand or collapse inventory
- expand the minimized interface
- use a skill
- cast or cancel a spell
- equip or unequip a staff
- disarm
- close a dialog
- close the game client

The planner can reject an intent when the snapshot is missing, belongs to a
different client, shows the wrong panel or interface mode, cannot resolve the
requested slot or target, or indicates that the operation is already satisfied.
Unsupported and rejected plans do not send partial input.

The dispatcher verifies the window target before sending messages. Its result
distinguishes issued, rejected, failed, and partially issued input.

`CloseClientIntent` is a terminal control intent and does not need slot or
target planning. When a configured map or coordinate change requests it, the
engine stops the macro and emits the intent. Interop verifies that the window
still belongs to the expected process, then posts `WM_CLOSE`, matching the
behavior of the former macro implementation.

```text
 configured map or coordinate change
        |
        v
 engine stops macro and emits CloseClientIntent
        |
        v
 ClientRuntimeHost verifies process and window
        |
        v
 guarded WM_CLOSE dispatch
```

If the close request cannot be issued, the host reports a runtime failure rather
than silently claiming that the client was closed.

## Pending actions and observation confirmation

Issuing input is not the same as confirming that the game accepted it. The
engine tracks client operations as `PendingAction` values.

```text
 engine creates intent and PendingAction
        |
        v
 Interop plans and dispatches input
        |
        v
 ClientActionIssueObserved
        |
        +-> rejected or failed: engine handles failure
        |
        `-> issued: PendingAction records issue time
                         |
                         v
                 newer ClientSnapshot
                         |
                         +-> expected state observed: complete
                         |
                         `-> deadline elapsed: retry or fail
```

Each pending action has:

- a unique action identifier
- request and deadline timestamps
- current and maximum attempt counts
- the snapshot sequence that existed before the action
- an issue timestamp after Interop reports successful dispatch

A snapshot can confirm an action only when it is newer than the baseline and
its capture began after the action was issued. This prevents an older in-flight
capture from falsely confirming a click or key press.

Panel changes, inventory expansion, staff equipment, skill use, spell casting,
target selection, and dialog handling all build on this pattern.

## Automation cycles and prioritization

Commands and new observations can raise an `AutomationCycleRequested` event.
The cycle examines immutable state and chooses the next eligible operation.
Only one client action can be pending at a time, but longer workflows retain
their own typed progress state.

The automation state includes:

- lifecycle and stop reason
- current configuration and queue snapshots
- spell and skill cooldown state
- pending panel and interface transitions
- staff selection and equipment transitions
- active skill-use and spell-cast workflows
- flower schedules and active flower workflow
- spell and flower target rotations
- dialog state and panel restoration
- the latest client snapshot and shared roster

Flowering and ordinary spell casting share the spell-cast workflow. The
flowering settings decide when a ready flower operation takes priority, while
the engine preserves queue identity and schedules so both kinds of work can
interleave without pretending the other operation failed.

Queue edits are converted to complete immutable runtime configuration and sent
as commands. The engine synchronizes queue identifiers, rotations, and flower
schedules, and cancels only an in-progress operation whose referenced entry was
removed or materially changed.

## App and MVVM projection

`ClientRuntimeViewModel` pumps host capture observations and
`MacroViewSnapshot` values onto the WPF dispatcher. `ClientListItemViewModel`
uses those values for:

- client identity, map, and vitals
- runtime health and diagnostic details
- macro lifecycle and command availability
- inventory, equipment, skill, and spell presentation
- queue activity, cooldown, health-wait, and flower timer feedback

The same runtime snapshot drives both automation and visible client data. The
App does not maintain a second memory reader.

Editable macro state is intentionally separate from observed client state.
`ClientMacroConfiguration` owns the user's selected skills and queue entries.
`RuntimeAutomationSetupFactory` combines that configuration, application
settings, character class, ability metadata, and staff metadata into immutable
Runtime configuration.

Persistence serializes the editable configuration. Transient runtime values,
such as pending actions, cooldown observations, active casts, and flower timer
progress, are rebuilt from runtime state and are not written to macro files.

### Current App boundary

Live identity, login state, vitals, map location, and flower-spell availability
are read directly from runtime snapshots by `ClientListItemViewModel`.
`ClientListViewModel` uses those runtime-backed values for visibility, sorting,
login and logout transitions, target-character lists, and selected-client
presentation. There is no secondary App-owned snapshot projection.

`ClientSession` retains only process attachment information and user-owned
session preferences such as the assigned hotkey and selected tab. It does not
contain copied memory state. Mutable WPF projections of immutable inventory,
equipment, skill, and spell snapshots live under `ViewModels.Presentation`.
User-editable spell, flower, and target entries live under
`ViewModels.Editing`.

Some window-specific interaction remains in WPF code-behind. New presentation
logic should go into view models, bindings, templates, converters, or focused
services. Code-behind should remain limited to interactions that require a WPF
window, routed input event, dialog owner, or Win32 window handle.

### Dependency injection and ownership

`App.xaml.cs` is the application composition root. It uses the .NET dependency
injection container to construct the main window and shared services. The
container is private to `App`; feature code receives dependencies through
constructors instead of resolving them through `App.Current`, a custom service
provider, or static manager instances.

Application-wide catalogs, settings, registries, file archives, logging, and
client process discovery are singletons because they have one deliberate owner
and lifetime. The main window is also a singleton because SleepHunter has one
application shell. Windows publish their constructor-injected catalogs and
settings as local XAML resources. Shared data-template dictionaries use local
binding proxies populated by the composition root because compiled merged
dictionaries cannot resolve resources added to the parent application at
runtime.

Per-client runtime hosts are not container singletons. `ClientRuntimeRegistry`
creates and owns one runtime per detected client, then disposes it when the
client leaves. Window-handle and dispatcher-dependent helpers are composed at
the WPF window boundary because their lifetime belongs to that window.

DI does not own immutable runtime snapshots, events, intents, or actions. Those
are values passed through the runtime pipeline. It also does not turn every
class into an interface. Interfaces are used at operating-system, persistence,
input, and interaction boundaries where tests or platform implementations need
substitution.

## Intentional compatibility boundaries

The legacy App engine, mutable memory-backed player models, custom service
container, and duplicate client-update loop have been removed. The following
compatibility paths remain intentionally:

- Persistence can import legacy `.sh4` files, then save current `.sh4x` files.
- `IO/Process` is used only by guarded launcher patches against a newly created,
  suspended client. It is not an alternative runtime observation path.
- Window code-behind handles WPF routed events, dialog ownership, drag and drop,
  and Win32 window handles. Runtime state, editable macro state, commands, and
  presentation state remain in their respective runtime or view-model layers.

## .NET 10 baseline

The solution and runtime tests target .NET 10. The engine was designed before
that upgrade, but its core choices already fit the current framework:

- `TimeProvider` and `MacroClock` make time deterministic.
- channels serialize session input without manual polling threads.
- immutable records and collections keep decisions isolated.
- current guard helpers validate arguments and disposed state.
- Interop uses source-generated P/Invoke for the live runtime path.

The review found no .NET 9 compatibility branch or substitute collection,
timer, or synchronization primitive that should be replaced only because the
target changed. Framework-specific rewrites should be driven by measured
behavior or a concrete correctness improvement, not syntax churn in the
deterministic engine.

## Failure containment

Failures stay attached to the layer that can explain them:

- mapping and memory errors produce typed capture failures
- incoherent map transitions produce an expected transition result
- intent planning reports why an operation cannot be planned
- input dispatch reports issue status without claiming game-side completion
- engine invariant failures stop only the affected runtime
- App view models expose diagnostics and force unavailable runtime lifecycle
  presentation to stopped

Stopping or disposing a runtime cancels its scheduler, session, capture and view
pumps, pending waits, and command availability. One failed client runtime does
not mutate another client's engine state.

## Testing strategy

The architecture is designed for deterministic tests:

- Runtime tests use immutable snapshots, commands, events, and a manual clock.
- Interop mapping and parser tests use deterministic byte-backed memory sources.
- intent planner and dispatcher tests use controlled targets and message sinks.
- host tests verify the snapshot, event, intent, and action-result feedback loop.
- App tests verify dispatcher marshaling, presentation projection, persistence,
  command availability, and runtime failure reporting.

Changes to live mappings or input coordinates still require smoke testing with
the supported 7.41 client. Unit tests prove the control flow and safety rules,
but they cannot prove that a live client executable still uses the documented
layout.

## Where to start in the code

- Runtime composition: `src/SleepHunter.Interop/Hosting/ClientRuntimeHost.cs`
- Windows attachment: `src/SleepHunter.Interop/Hosting/WindowsClientRuntimeFactory.cs`
- snapshot orchestration: `src/SleepHunter.Interop/Snapshots/ClientSnapshotCapture.cs`
- capture scheduling: `src/SleepHunter.Interop/Snapshots/ClientSnapshotScheduler.cs`
- event loop: `src/SleepHunter.Runtime/Hosting/MacroSession.cs`
- decision entry point: `src/SleepHunter.Runtime/Engine/MacroEngine.cs`
- decision validation: `src/SleepHunter.Runtime/Engine/MacroDecisionInvariants.cs`
- input planning: `src/SleepHunter.Interop/Input/ClientIntentPlanner.cs`
- input execution: `src/SleepHunter.Interop/Input/ClientIntentExecutor.cs`
- App composition root: `src/SleepHunter.App/App.xaml.cs`
- App runtime registry: `src/SleepHunter.App/Services/Runtime/ClientRuntimeRegistry.cs`
- runtime view model: `src/SleepHunter.App/ViewModels/ClientRuntimeViewModel.cs`
- client presentation: `src/SleepHunter.App/ViewModels/ClientListItemViewModel.cs`
