# SleepHunter Runtime Refactor

## Status

This is the living engineering plan for the SleepHunter runtime and application
refactor. Work begins on the `codex/runtime-refactor` branch and will be delivered
through a series of focused pull requests.

The next release is expected to contain the completed refactor. There is no
planned interim release containing repairs to the legacy macro engine. Every
pull request must still build successfully and include automated validation for
the behavior it introduces.

The deterministic runtime is now the only macro execution path, client runtime
hosts are attached through Interop, and the main toolbar, queues, persistence,
hotkeys, polling, and client launch are connected through tested application
services and Community Toolkit commands. The branch is ready for the live
Windows and client validation in [SMOKE_TESTING.md](SMOKE_TESTING.md).
Remaining MainWindow code-behind is limited to view gestures, native window
integration, secondary dialogs, and application-shell coordination.

The July 25 application audit removed unreferenced helpers, events, converters,
collection APIs, and the unused world-entity model. The latter had no runtime
or UI consumer and performed a second client-memory traversal beside Interop.
The remaining legacy `Player` process reads are still active inputs for the
inventory, equipment, skill, and spell panes, so they remain in the application
until those panes consume Interop projections. Client patch planning is also
still used by `ClientLaunchService`; extracting it into
`SleepHunter.Patching` remains a focused follow-up rather than being mixed into
dead-code removal.

## Executive Decision

SleepHunter will receive a new unit-tested runtime built beside the legacy
implementation. The new runtime will be designed as if the automation engine
were being built today:

- Deterministic state transitions.
- A single owner for mutable macro state.
- Immutable, sequenced client snapshots.
- Channels for reliable commands and coalesced observations.
- Explicit intents, pending actions, deadlines, and failure outcomes.
- Virtual time and simulation support.
- No WPF, process-memory, window-input, or scripting dependencies in engine
  decisions.

The legacy engine is a behavioral reference, not the foundation of the new
design. It will remain largely unchanged until the replacement is integrated,
then it will be removed.

## Goals

1. Replace the polling and lock-based macro engine with a deterministic event
   loop.
2. Make macro behavior testable without a running game client.
3. Separate process interoperability from automation decisions.
4. Move application behavior out of WPF code-behind and into ViewModels and
   application services.
5. Make all waits cancellable and bounded without blocking threads.
6. Preserve supported client mappings and safe memory-reading behavior.
7. Preserve or explicitly migrate user macro state and settings.
8. Leave a safe extension point for future Lua scripting.
9. Keep the codebase understandable through explicit project and dependency
   boundaries.

## Non-Goals

- Repairing or incrementally redesigning the legacy macro engine for an interim
  release.
- Reproducing legacy implementation details when only observable behavior
  matters.
- Polling as quickly as the CPU permits.
- Removing every lock or concurrent collection regardless of its purpose.
- Adding Lua scripting during the initial runtime construction.
- Allowing scripts to access raw memory, window input, or patching APIs.
- Moving legacy code into temporary abstractions solely to make it appear
  modern.

## Current Problems Being Replaced

The current application combines several responsibilities that need independent
ownership:

- `MainWindow.xaml.cs` owns process discovery, client updates, flower timing,
  macro lifecycle, serialization, updates, native hooks, and UI state.
- Each macro runs an open-ended polling task.
- Process discovery, client updates, and flower timers run as additional
  background workers.
- Spell and flower queues are mutable lists guarded by reader-writer locks.
- Client operations wait through repeated checks and `Thread.Sleep`.
- Deferred callbacks can mutate runtime state outside an explicit state
  transition.
- UI event handlers read and modify macro state directly.
- Static managers make dependencies and lifecycle ownership difficult to test.

Known legacy defects, such as unbounded equipment waits and nullable
class-filter behavior, will be captured as required outcomes in new engine tests
rather than repaired in the legacy engine.

## Initial Project Structure

The refactor started with two production projects and their matching test
projects:

```text
SleepHunter.Runtime
SleepHunter.Interop

SleepHunter.Runtime.Tests
SleepHunter.Interop.Tests
```

`SleepHunter.Persistence` and `SleepHunter.Persistence.Tests` are added when
versioned macro configuration and legacy migration become active work. Keeping
that adapter separate prevents XML, file-system, and format-version concerns
from entering the deterministic runtime.

The WPF application is named `SleepHunter.App` and lives beside the other
production projects under `src/`. Its product name, namespaces, and shipped
executable remain `SleepHunter`. `SleepHunter.Updater` is also under `src/` as
a separate executable project.

Additional assemblies will be created only when their active implementation
justifies a dependency boundary. Likely later candidates are:

```text
SleepHunter.Patching
SleepHunter.Scripting.MoonSharp
```

Avoid generic dumping-ground projects such as `Common`, `Shared`, or
`Infrastructure`.

### SleepHunter.Runtime

`SleepHunter.Runtime` targets plain `net10.0` and has no dependency on another
SleepHunter project. It contains both the pure engine and its asynchronous host,
organized by responsibility:

```text
Engine/
Sessions/
Scheduling/
Observations/
Actions/
```

The engine area contains deterministic state, events, decisions, and intents.
The session area contains channels, scheduling, lifecycle, and intent dispatch.
Keeping them in one assembly avoids premature project fragmentation while tests
and namespace rules preserve the logical boundary.

`SleepHunter.Runtime` must not reference:

- WPF or a UI dispatcher.
- Win32 or process APIs.
- Client memory mappings.
- File dialogs or application windows.
- MoonSharp or another scripting implementation.
- The legacy `Player`, `MacroState`, or static manager types.

### SleepHunter.Interop

`SleepHunter.Interop` owns interoperability with the external game client. Its
name intentionally does not include the explicit game name.

Expected responsibilities include:

```text
Processes/
Memory/
Input/
Snapshots/
Mappings/
Win32/
```

It discovers and attaches to client processes, performs bounded memory reads,
builds immutable runtime snapshots, sends guarded window input, and translates
between raw client data and runtime observation or action types.

`SleepHunter.Interop` targets Windows and depends on `SleepHunter.Runtime`.

### SleepHunter.Persistence

`SleepHunter.Persistence` targets plain `net10.0`, depends only on
`SleepHunter.Runtime`, and owns:

- The immutable persisted macro configuration boundary.
- The current versioned macro file schema.
- Bounded and DTD-prohibited XML reading.
- Atomic current-format file replacement.
- Legacy `.sh4` import and explicit migration diagnostics.

It does not persist transient engine state such as pending actions, cooldowns,
snapshot observations, or target rotation cursors.

### Dependency Direction

```text
SleepHunter.App
  -> SleepHunter.Runtime
  -> SleepHunter.Interop
  -> SleepHunter.Persistence

SleepHunter.Interop
  -> SleepHunter.Runtime

SleepHunter.Persistence
  -> SleepHunter.Runtime

SleepHunter.Runtime
  -> no SleepHunter project dependencies
```

If patching is extracted later:

```text
SleepHunter.Patching
  -> SleepHunter.Interop
```

Library projects must never reference the WPF application. `SleepHunter.App`
may reference the library projects, keeping dependency flow inward.

## Dependency Policy and Baseline Audit

Dependencies are part of the architecture and must be audited before the new
projects establish their package graph. The audit includes:

- Direct and transitive NuGet packages.
- Project, framework, SDK, analyzer, and source-generator references.
- Native libraries and operating-system requirements.
- Build, test, publish, documentation, and release tooling.
- Generated files that accidentally appear to declare dependencies.

Prefer the .NET shared framework and BCL when they provide the required API.
Every added package must have a documented purpose, owning project, license,
maintenance status, security status, and removal strategy. A package must not
be referenced by every project solely for convenience.

Use central package management when the new projects begin adding packages.
Test-only dependencies and analyzers must remain private to test or build
projects where possible.

### Baseline Findings

The July 24, 2026 baseline audit found six distinct direct NuGet packages and
eight project-level package references across the real projects.

`SleepHunter.App` and `SleepHunter.Updater` previously each referenced:

- `Microsoft.CSharp` 4.7.0.
- `System.Data.DataSetExtensions` 4.5.0.

No source usage of `dynamic`, the C# runtime binder, `System.Data`, `DataSet`,
`DataTable`, or the DataSet extension APIs was found. Both packages were
removed because the relevant framework surface is supplied by the platform.
The complete solution and both published executables verified their removal.

`SleepHunter.App.Tests` directly references:

- `Microsoft.NET.Test.Sdk` 18.8.1.
- `NUnit` 4.6.1.
- `NUnit.Analyzers` 4.14.0.
- `NUnit3TestAdapter` 6.2.0.

These packages have active and distinct test execution, framework, analysis,
and adapter responsibilities. They were updated together during the .NET 10
migration and verified across every test project. All four use the MIT license
and are actively maintained by Microsoft or the NUnit project. The test SDK and
adapter are required for test discovery and execution, NUnit owns the test API,
and the analyzers enforce NUnit correctness. They can be removed only if the
test stack is replaced, except that the analyzer can be removed independently
if its build-time checks no longer provide value.

The transitive test packages, including test-platform telemetry, coverage,
Application Insights, and Newtonsoft.Json components, arrive through the test
SDK and adapter. They are not application runtime dependencies and should not
be referenced directly unless a future feature independently requires them.

The July 25, 2026 .NET 10 audit retained
`CommunityToolkit.Mvvm` 8.4.2 as the latest stable release and updated all four
test packages to the latest stable versions listed above. A solution-wide
NuGet audit then reported no outdated, vulnerable, or deprecated direct
packages. Runtime, Interop, Persistence, and Updater continue to have no direct
NuGet dependencies.

`Microsoft.NET.ILLink.Tasks` is automatically supplied by the SDK for the
single-file publish configuration. It is not an explicit application package.

The current NuGet sources reported no known vulnerable direct or transitive
packages and no deprecated direct packages at audit time.

The current project graph follows the dependency direction documented above.
The WPF application references Runtime and Interop, Interop and Persistence
each reference Runtime, and every test project references only its system under
test.

A previously tracked generated file,
`SleepHunter.Updater/SleepHunter.Updater_0ypjaz0w_wpftmp.csproj`, contained stale
machine-specific .NET 7 reference paths and an apparent
`Microsoft.Windows.Compatibility` dependency. It is not part of the solution or
the real updater dependency graph. It was removed, and WPF temporary project
files are ignored.

GitHub Dependabot previously reported that generated project as the source of a
high-severity `GHSA-555c-2p6r-68mm` alert because it references
`Microsoft.Windows.Compatibility` 7.0.1. The real solution restore does not
include that project, which is why the solution-level NuGet vulnerability audit
did not report the package. Removing the generated project avoided preserving a
dependency that the real updater does not use.

### Audit Procedure

Dependency changes should follow this sequence:

1. Confirm actual source, build, test, or publish usage.
2. Remove or update one coherent dependency group at a time.
3. Restore and build the complete solution.
4. Run all automated tests.
5. Publish the affected executable projects and inspect the output.
6. Run focused application or updater validation when build-time proof is
   insufficient.
7. Record intentional dependencies and rejected alternatives.

CI should regularly report vulnerable, deprecated, and outdated direct and
transitive packages. An update being available does not by itself justify
changing a package inside unrelated behavior work.

## Runtime Model

### Core Values

The runtime will model behavior using immutable values with explicit meaning:

- `ClientSnapshot`: the latest coherent client observation.
- `MacroCommand`: a reliable external request such as start, stop, or queue
  modification.
- `MacroEvent`: an observation, deadline, action result, or lifecycle event.
- `MacroState`: all state exclusively owned by one macro session.
- `MacroDecision`: a deterministic state transition and its resulting intents.
- `MacroIntent`: a requested external operation.
- `PendingAction`: an issued operation awaiting confirmation or failure.
- `MacroViewSnapshot`: immutable state published to UI and scripting consumers.

Final names may change as the model becomes concrete. Their responsibilities
must remain distinct.

### Pure Engine

The core decision function is conceptually:

```text
Decide(current state, input event, current time)
    -> new state
    -> zero or more internal events
    -> zero or one external client intent
    -> next deadline
```

Given the same state, event, snapshot, and time, it must produce the same
result. It may not read wall-clock time, client memory, settings files, or WPF
state.

The engine should emit at most one exclusive external client intent before
waiting for a new event or observation. This makes input ordering and action
confirmation explicit.

### Macro Session

Each attached player receives one `MacroSession`. That session is the only
writer for its mutable runtime state.

The session owns:

- A reliable command channel for lifecycle, settings, and queue changes.
- A latest-value snapshot mailbox where stale observations are coalesced.
- A priority queue of scheduled engine events.
- Pending action and deadline state.
- Cancellation and awaited shutdown.
- Publication of immutable view snapshots.

Commands must not be dropped. High-frequency snapshots may be coalesced because
only the newest valid observation is useful.

Channels do not make mutable objects safe automatically. Values crossing the
session boundary must be immutable or exclusively owned by the session.

### Event Loop

The runtime uses a scheduled event loop rather than a continuously spinning
frame loop. A session wakes when:

- A command arrives.
- A newer client snapshot arrives.
- A scheduled deadline becomes due.
- An optional low-frequency heartbeat becomes due.

One iteration:

1. Drains available reliable commands in a defined order.
2. Accepts the newest valid snapshot and rejects stale snapshots.
3. Advances the deterministic state machine.
4. Dispatches any permitted client intent.
5. Publishes an immutable view snapshot when observable state changes.
6. Calculates the next deadline and awaits the next input.

No engine path should use `Thread.Sleep`, synchronous task waiting, or an
unbounded polling loop.

Runtime-owned automation is disabled by default and enabled through one
immutable `AutomationConfiguration`. Accepted snapshots, roster observations,
queue changes, and start or resume transitions can raise one immediate
automation-cycle event. A cycle evaluates flowering and spells in the
configured order, then skills, and stops after producing one bounded intent.
The cycle does not add a second timer. Snapshot capture cadence provides the
normal observation tick, while scheduled events remain responsible for action
deadlines and deferred dialog work.

After a spell or skill action completes, automation waits for a coherent
snapshot whose capture began after the completed action. Repeated cycles that
produce identical planning state are collapsed without another state revision
or view publication.

## Snapshot and Observation Strategy

Snapshot capture and engine execution are independently scheduled. The engine
must not call `ReadProcessMemory` directly.

Every snapshot contains:

- A sequence number.
- Capture start and completion timestamps.
- Client instance identity.
- Validity or quality information.
- Explicit freshness for independently captured sections when applicable.

A newer snapshot must be required to confirm an action. For example, equipment
data captured before an equip action cannot confirm that action.

Snapshot capture follows these rules:

- Only one capture may run per client at a time.
- Slow captures cause later observations to be skipped or coalesced, never
  queued into a backlog.
- Reads remain bounded and match the single configured binary layout.
- Mutable collection roots or generation values are revalidated when
  available.
- Partial or incoherent snapshots are rejected.
- Legacy compact mappings remain available where richer pane data is
  unavailable unless their removal is explicitly validated.

The initial observation cadence will be configurable and chosen through
measurement. Instrumentation should record:

- Total capture duration.
- Duration by snapshot section.
- Read count and total bytes.
- Failed or invalid reads.
- Median, high-percentile, and maximum capture times.

If measurements justify it, observations may be separated into:

- Fast action-confirmation fields.
- Normal player state.
- Slow or on-demand inventory and ability collections.

The runtime may temporarily request faster relevant observations while waiting
for an action confirmation and slower observations while idle or waiting
through a long deadline.

## Actions and Deferred Work

Client actions never wait for completion inside the executor.

The action sequence is:

1. The engine records pending state with a deadline and emits an intent.
2. Interop executes the intent and returns an issuance result.
3. The host reports that result through the session's reliable action issue
   channel.
4. Only an issued result allows a newer snapshot or the action deadline to
   confirm completion.
5. The engine chooses the next deterministic transition.

If issuance feedback is missing at the deadline, the action is marked timed
out. Rejected, unsupported, failed, partially issued, and feedback-timeout
results clear the pending action, mark its owning workflow as issue failed, and
pause the macro. The runtime does not automatically retry these outcomes.
Partial issuance is especially uncertain because the client may have processed
only part of the input sequence.

Before posting window input, interop must verify that the target HWND still
exists, is owned by the expected process, and retains the client-area
dimensions used to plan coordinates. Input plans contain only an explicit,
bounded set of keyboard and mouse messages. A native post failure before any
intended message is a failed issuance, while failure after one or more messages
is a partial issuance. Partial plans run bounded best-effort key and mouse
release cleanup, but still remain uncertain and must never be reported as
success. A successfully posted plan means only that Windows accepted the
messages, not that the client completed the action.

For the supported client layout, basic client actions are translated from
semantic intents into input plans. Cancel dialog uses Escape, disarm uses the OEM
tilde key, and assail uses Space. Panel changes and skill activation use the
documented 640 by 480 client coordinates, scaled independently to the guarded
client width and height. Panel changes preserve the Temuair and Medenia Shift
selection behavior. Skill slots are normalized with
`((absoluteSlot - 1) % panelCapacity) + 1`, which keeps exact capacity
boundaries in the final visible slot. Mouse button-up messages use a zero
button-state parameter.

Intent planning reports planned, rejected, or unsupported before native input
is attempted. Issuance then reports issued, rejected, failed, or partially
issued and retains the planning result. A rejection may therefore identify
either invalid snapshot context with no dispatch, or a valid input plan rejected
by the HWND guard with dispatch diagnostics. Equipment input additionally
requires the inventory panel, the correct observed inventory display mode, and
the expected staff name in the requested slot.

Cast-spell input requires the expected panel and spell name in the requested
slot. Client spell input double-clicks the verified spell slot, then
immediately clicks a projected target when one is required. Self, logical
screen-point, relative-tile, and absolute-tile targets use the legacy 640 by 480
projection. The logical target is scaled to the guarded client dimensions
before its pixel offset is applied. Absolute targets additionally require an
observed map location and remain bounded to the supported local tile range.
The deterministic runtime resolves character targets to relative tiles from an
immutable, coherent client roster before input planning. It rejects missing,
logged-out, moved, different-map, and out-of-range targets without consuming an
action identifier. The client input planner still rejects an unresolved character
target defensively. Area targets are likewise resolved to one tile by the
runtime before input planning.

Pending actions contain enough information to diagnose and test behavior:

- Action identifier and kind.
- Requested timestamp, issuance state, and issued timestamp.
- Expected confirmation condition.
- Deadline.
- Attempt count.
- Last issuance result, failure outcome, and cancellation outcome.

Deferred work is represented as scheduled engine events. Arbitrary callbacks
must not mutate macro state outside the session loop.

## Concurrency Decisions

Modern .NET concurrency features will be used according to ownership:

- `Channel<T>` for reliable session commands and bounded streams.
- `TimeProvider` for deadlines, cooldowns, and virtual-time tests.
- `PriorityQueue<TElement, TPriority>` for scheduled engine events.
- `PeriodicTimer` where periodic observation is appropriate.
- `CancellationToken` for all long-lived runtime and interop operations.
- Immutable records and collections for published data.
- `ConcurrentDictionary` only for registries of independent sessions or
  resources.

The goal is not lock-free code. Short locks may remain appropriate for isolated
image caches or native resources. Locks must not coordinate multi-step macro
state transitions.

Avoid:

- Fire-and-forget tasks.
- `async void` outside unavoidable WPF event handlers.
- Blocking task waits.
- Multiple writers to one session's state.
- Using concurrent collections as a substitute for a transaction or state
  machine.

## Behavior Construction Order

The new engine will be implemented in complete, simulated behavior slices:

1. Session lifecycle: start, pause, resume, stop, logout, and cancellation.
2. Queue commands and deterministic rotation.
3. Panel transitions and bounded action confirmation.
4. Class-aware staff selection and equipment transitions.
5. Spell selection, mana requirements, casting, and cooldowns.
6. Skills, assails, health conditions, and disarm requirements.
7. Flowering and alternate-character coordination.
8. Macro-state persistence and compatibility migration.

Required staff behavior includes:

- Never request equipment restricted to another class.
- Treat an unknown character class as unable to use class-specific equipment.
- Permit explicitly class-neutral equipment.
- Bound and cancel every equipment transition.
- Surface a stable failure or retry outcome without stalling the session.

## Testing Strategy

### Pure Unit Tests

Test individual policies and transitions without threads or a live client:

- Lifecycle transitions.
- Queue ordering and rotation.
- Staff compatibility and selection.
- Spell and skill eligibility.
- Mana, health, and cooldown conditions.
- Action confirmation, timeout, retry, and cancellation.
- Timer wraparound and virtual-time progression.

### Scenario Simulation

A simulation harness provides scripted snapshots, commands, and virtual time.
Tests assert the exact intents requested and the states published over a
complete scenario.

Examples include:

- Equipping a staff and casting after confirmation.
- Equipment never confirming before its deadline.
- Pausing while an action is pending.
- Logging out during a cast or panel transition.
- Editing a queue while its current item is active.
- Flowering across multiple simulated clients.

### Invariants

The runtime must continuously satisfy invariants such as:

- No client action while stopped or paused.
- No incompatible staff selection.
- Every pending action has a deadline.
- No stale snapshot confirms a newer action.
- No partial snapshot changes engine state.
- Logout clears or cancels pending work.
- Queue indices always refer to valid items.
- Advancing virtual time eventually reaches a stable wait state.

### Runtime Concurrency Tests

A smaller test layer exercises the channel-driven session:

- Reliable command ordering.
- Snapshot coalescing.
- Concurrent command producers.
- Cancellation and awaited disposal.
- No intents emitted after shutdown.

### Interop Tests

Interop tests use deterministic byte arrays and fake process readers:

- Version-specific field parsing.
- Bounded strings and collections.
- Invalid pointers, counts, and generation changes.
- Coherent snapshot acceptance and rejection.
- Action translation without sending live input.
- Snapshot timing instrumentation.

### Trace Replay

Read-only snapshot traces captured from a supported client may be replayed
through the simulator. This provides realistic state changes and timing without
requiring a live client in automated tests.

### Live Client Validation

Complete live validation occurs after runtime and WPF integration. It must cover
the supported 7.41 client and explicitly identify any other client variant
tested.

Manual validation complements automated tests and includes:

- Long-running sessions.
- Multiple attached clients.
- Login, logout, reconnect, and process exit.
- Start, pause, resume, stop, and application shutdown.
- Queue editing during automation.
- Staff switching across character classes.
- Failed and delayed client transitions.
- Patcher signature and failure behavior.

## WPF and MVVM Plan

The WPF refactor begins after runtime commands and published snapshots are
stable. This avoids building ViewModels against the legacy mutable API.

ViewModels consume:

- Immutable runtime view snapshots.
- Async commands that post reliable runtime commands.
- Application services for dialogs, navigation, persistence, and updates.

`SleepHunter.Interop` provides one `ClientRuntimeHost` per attached client. The
host composes a runtime session, independent snapshot scheduler, semantic
intent executor, and guarded window-target provider. It forwards only complete
snapshots into the runtime, retains failed capture diagnostics, clears the
executable snapshot when the newest capture fails, executes intents only
against the newest coherent snapshot, and reliably reports every
issuance outcome. Capture and intent pumps are cancellable and disposal awaits
the scheduler, pumps, and runtime session. It also publishes a bounded
latest-value stream of immutable capture observations. Each observation pairs
the successful or failed result with its rolling timing and memory-read
statistics, so slow consumers cannot create an unbounded diagnostics backlog.

The WPF `ClientRuntimeViewModel` owns that host, marshals immutable views through
an injected UI dispatcher, exposes capture health, errors, snapshots, and
statistics through Toolkit-generated C# 14 observable partial properties, and
exposes source-generated Toolkit async relay commands for macro lifecycle
changes. Generated dependent-property notifications update capture projections
and invalidate lifecycle commands. Runtime-fed properties keep private setters
so only the host pumps can publish them. Feature ViewModels may forward
additional typed runtime commands through the same boundary. The legacy engine
remains authoritative until client attachment and the corresponding UI slices
are explicitly cut over.

`ClientRuntimeRegistry` now attaches an active host for each discovered client.
The Windows factory still opens only query and virtual-memory-read process
rights. It captures all snapshot sections at the configured client update
interval and enriches abilities from an immutable application metadata catalog.
All attached clients share one monotonic macro clock, so snapshot, action, and
cross-client flower timestamps have the same origin.

The registry projects changed client observations into one content-deduplicated
roster and publishes it to every host. Roster feedback cannot produce an
unbounded view loop because an identical projection is not republished.
MainWindow owns the registry, detaches hosts when clients disappear, and awaits
disposal before shutdown. The host can now accept lifecycle commands, but the
legacy toolbar remains authoritative until the automation configuration and
toolbar slices are cut over.

The client list is the first vertical WPF slice. `ClientListViewModel` owns a
stable, ordered collection of `ClientListItemViewModel` instances. Each item
projects the coherent runtime character, presence, location, and vitals
sections into the existing client card. Missing sections, unsupported clients,
and the newest failed capture fall back to the corresponding legacy
observation, while a small runtime badge exposes capture status. The client
list owns the selected item through a generated observable property and a
two-way XAML binding, clearing selection when its client is removed. Inventory,
equipment, ability panes, and secondary window operations continue to unwrap
the legacy `Player` until their own slices are cut over.

The spell and flower queue editor is now a separate vertical slice.
`MacroEditorViewModel` exposes source-generated Toolkit commands for selected
entry removal and queue clearing, invalidates them when the runtime changes
editing availability, and delegates deterministic reordering to the editable
configuration. Read-only observable queue projections replace custom queue
events and manual `Items.Refresh` calls. Rotation and flowering options use
two-way bindings. MainWindow retains only target-dialog and drag gesture
mechanics for these queues.

The transition-only runtime configuration view model has been removed. Loading
a macro file updates the authoritative editable configuration only. Start and
resume map that complete typed configuration into immutable queues and runtime
policies before issuing the lifecycle command, so an idle runtime no longer
receives a redundant second queue copy during file load.

MainWindow will be decomposed by responsibility rather than replaced with one
large ViewModel:

- Client list and selection.
- Macro toolbar and lifecycle.
- Spell queue.
- Flower queue.
- Inventory, skills, and spells.
- Features.
- Application launch and update operations.

Code-behind remains acceptable only for view mechanics such as window chrome,
focus, drag gestures, and native window integration. It must not contain macro
decisions, process polling, persistence orchestration, or application state
ownership.

Macro file persistence now follows that boundary. A tested application service
owns current saves, bounded loads, legacy autosave discovery and migration,
broken-autosave cleanup, editable-configuration mapping, and imported hotkey
replacement. A focused macro-persistence ViewModel exposes Community Toolkit
commands for load, save, and spell-queue visibility. WPF owns only the
file-dialog and message adapter, while `MainWindow` calls the same application
boundary for login, logout, and shutdown autosaves.

The remaining application-shell polling now runs through a focused client
polling coordinator. Process discovery and legacy client observation retain
independent, dynamically read cadences, execute immediately on startup, use an
injected `TimeProvider` for deterministic tests, and marshal only process
reconciliation to the UI dispatcher. Cancellation replaces
`BackgroundWorker` recursion and `Thread.Sleep`, and shutdown awaits both loops
before saving macro state or disposing runtime hosts.

Global hotkey editing now follows the same boundary. WPF translates a key
gesture and reports errors, while a tested application service owns uniqueness,
native registration transfer, model updates, and rollback. A failed assignment
retains the previous character binding, and a failed transfer attempts to
restore the displaced registration before returning control to the view.

Client launch now follows a matching boundary. `ClientLaunchViewModel` exposes
the launch button through a generated Community Toolkit command and enables it
only after the shared client layout loads successfully. `ClientLaunchService`
snapshots mutable settings, selects only requested and supported patches, owns
the suspended process and native handles, verifies executable and patch bytes,
and either resumes a fully patched client or terminates a failed launch. WPF
owns only the error presentation adapter.

The pre-runtime `PlayerInterfaceExtender` and `WindowAutomator` have no remaining
callers and are removed instead of being converted to async polling. Semantic
input planning and execution now belong exclusively to `SleepHunter.Interop`,
where later snapshots confirm equipment and panel transitions. Removing that
stack also removes its blocking 16 ms waits, direct `PostMessage` input, UI
coordinate helpers, deferred-action residue, and adjacent unreferenced
utilities.

New WPF ViewModels will use
[CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
where its focused building blocks apply. Planned uses include:

- `ObservableObject` and generated observable properties.
- Generated relay commands for synchronous UI requests.
- Generated async relay commands with propagated cancellation tokens.
- Generated command invalidation when observable state changes.

The toolkit may be adopted incrementally, so legacy observable models do not
need to be converted before they are replaced. Runtime state and services must
not depend on the toolkit. Messenger-based communication will be used only when
direct command, snapshot, or service relationships are insufficient.

`CommunityToolkit.Mvvm` 8.4.2 is centrally pinned and referenced only by the WPF
application. The July 25, 2026 package audit identified it as the current,
non-deprecated MIT release. Its purpose is observable ViewModels and relay
commands. Runtime, Interop, and Persistence remain independent of it. It can be
removed if the WPF layer is replaced or the small ViewModel surface no longer
benefits from the package.

## Future Lua Scripting

Lua scripting is a design constraint, not part of the initial runtime
deliverable. MoonSharp is the current candidate because it is a managed Lua
interpreter with coroutine and sandboxing support.

The runtime will not depend on MoonSharp. When scripting work begins, the
adapter may live in:

```text
SleepHunter.Scripting.MoonSharp
```

Built-in automation and scripts use the same validated intent and action
protocol. A script may observe immutable snapshots and yield requests such as:

- Cast a spell.
- Use a skill.
- Equip an item.
- Click a target.
- Wait until a condition.
- Wake after a duration.
- Pause or stop.

The runtime, not the script, owns action serialization, scheduling,
confirmation, deadlines, and cancellation.

A MoonSharp host must:

- Expose explicit proxy objects rather than raw runtime or framework objects.
- Use a restricted standard-library module set.
- Avoid automatic CLR registration.
- Use a custom or disabled script loader.
- Apply instruction and execution budgets.
- Integrate waits through coroutine yields and runtime deadlines.
- Prevent direct access to process memory, window input, patching, and the file
  system unless a future explicit capability grants it.

The script API will be versioned independently from internal runtime types.
The final scripting trust model remains an open decision.

## Patching

Client patching remains a separately controlled, high-risk subsystem. The
runtime may query client capabilities but must not apply patches.

When extracted, `SleepHunter.Patching` will own:

- Version and executable matching.
- Signature verification.
- Replacement byte generation.
- Safe application sequencing.
- Failure behavior that prevents partially patched launches.
- Tests for every supported patch and client variant.

Patching and runtime construction should not be combined into one large pull
request.

## Pull Request Roadmap

### PR 0: Plan and Repository Foundation

- Add this document.
- Create the refactor branch.
- Confirm project names and dependency rules.

### PR 1: Project Scaffolding

- Add `SleepHunter.Runtime` and `SleepHunter.Runtime.Tests`.
- Add `SleepHunter.Interop` and `SleepHunter.Interop.Tests`.
- Add shared build properties for new projects.
- Add central package management.
- Enable nullable reference types and implicit usings in new projects.
- Add an architecture dependency test or equivalent build rule.
- Remove the tracked WPF temporary project and ignore future generated
  temporary projects.
- Prove whether `Microsoft.CSharp` and `System.Data.DataSetExtensions` can be
  removed from both executable projects.
- Review and update the test package group in a focused commit if compatible.
- Add repeatable vulnerable, deprecated, and outdated package reporting.

### PR 2: Runtime Values and Simulator

- Add state, command, event, intent, decision, and snapshot primitives.
- Add virtual-time support.
- Add the pure step contract and scenario harness.
- Add lifecycle and invariant tests.

### PR 3: Session Host

- Add command channels and snapshot coalescing.
- Add scheduled events and pending actions.
- Add cancellation and awaited disposal.
- Add runtime concurrency tests.

### PR 4: Automation Behavior

- Implement behavior slices in the documented construction order.
- Add scenario and invariant coverage for every slice.
- Keep live client access out of the runtime.

### PR 5: Interop Snapshot Capture

- Implement bounded snapshot production.
- Add configured-layout parser tests.
- Add capture timing instrumentation.
- Perform read-only measurement against the supported client.
- Select initial observation cadence based on measurements.

### PR 6: Interop Action Execution

- Translate runtime intents into client input.
- Preserve guards and configured-layout behavior.
- Test action translation and cancellation without live input.

### PR 7: Persistence and Compatibility

- Define versioned macro configuration persistence.
- Import supported legacy macro-state files or document a deliberate migration.
- Add round-trip and migration tests.

### PR 8: WPF and MVVM Integration

- Compose runtime and interop services.
- Add and centrally pin CommunityToolkit.Mvvm.
- Build ViewModels against commands and immutable snapshots.
- Convert the application in vertical UI slices.
- Remove runtime ownership from MainWindow.

### PR 9: Cutover and Legacy Removal

- Make the new runtime authoritative.
- Remove the legacy macro engine, polling workers, queue locks, deferred
  dispatcher, and blocking client wait helpers.
- Remove unused static managers and custom UI threading helpers when they have
  no remaining consumers.

### PR 10: Complete Validation and Documentation

- Run the full automated suite.
- Run long-duration simulations and concurrency stress tests.
- Complete live-client validation.
- Update user documentation and `CHANGELOG.md`.

Patching extraction and Lua scripting may proceed as later focused efforts once
their prerequisites are stable. They do not block the initial runtime cutover
unless the final release scope explicitly includes them.

## Pull Request Rules

Every pull request in the refactor series must:

- Build successfully.
- Add or update tests for its behavior.
- Preserve unrelated user work.
- Keep new dependencies directed inward.
- Justify every new package and keep it scoped to its owning project.
- Avoid exposing partially initialized mutable state.
- Document new invariants or changes to this plan.
- Identify any behavior that still requires live-client verification.

The existing application should remain buildable throughout the series. The
legacy engine remains available until the explicit cutover pull request.

## Settled Decisions

As of July 24, 2026:

- Build a new runtime rather than incrementally repairing the legacy engine.
- Deliver the work through a series of pull requests on a refactor branch.
- Do not release a legacy-engine patch before the completed refactor.
- Start with `SleepHunter.Runtime` and `SleepHunter.Interop`.
- Use `SleepHunter.Interop`, not a game-specific assembly name.
- Support one unified Dark Ages client layout. Keep addresses and patch
  metadata configurable, but do not encode client-release or private-server
  names into Runtime or Interop types and do not route behavior by a version
  string.
- Use one `ClientLayout` model and one direct `ClientLayout.xml` root for
  launch, patch, process detection, legacy WPF reads, and Interop mappings.
  Do not retain a version selector, signature-based routing, collection
  wrapper, or `ClientVersion` compatibility model.
- Audit and minimize existing dependencies before establishing the new package
  graph.
- Keep pure decisions and channel-driven hosting in one runtime assembly
  initially.
- Route process memory through a read-only `IProcessMemorySource` and a
  per-capture `MemoryReadSession`. The session validates the complete address
  range, block size, total-byte budget, read-count budget, and pointer depth
  before issuing a transport read.
- Treat partial process-memory reads as failures and retain their actual byte
  count and native error code for diagnostics. Never parse a partial buffer.
- Keep process-handle ownership outside `WindowsProcessMemorySource` so client
  attachment and disposal have one explicit owner.
- Represent the client mapping as one immutable, case-insensitive map with
  explicit pointer width, value kind, base address, and signed pointer offsets.
  Resolve every pointer and offset through checked address arithmetic.
- Require one direct client layout in bounded, DTD-prohibited XML. Runtime
  client identity is instance-only and never selects a layout by version name.
  Addresses remain configurable without code changes. Preserve search-based
  mappings as explicit metadata and require a dedicated bounded search resolver
  before reading them.
- Publish a core snapshot only after the client session root,
  character ownership, active panel, inventory display mode, and map location
  remain stable across the capture. A null session is a complete logged-out
  observation. Transport failures, invalid field values, and changed state or
  ownership produce diagnostics and metrics, but never a partial snapshot.
- Read the supported client's bounded compact inventory table for deterministic
  slot/name observations, excluding the synthetic gold slot. Prefer the
  coherent equipment snapshot for weapon and shield observations, but preserve
  the bounded compact equipment-name table as a fallback. Revalidate every
  collection root before publishing its parsed section. Keep these sections
  opt-in so macros that do not need staff or disarm state are not blocked by an
  unrelated collection.
- Prefer supported skill and spell pane snapshots for slot, level, cast-line,
  and client action-delay observations. Revalidate the pane capacity address
  and value, pointer-table root, and complete pointer table before publishing.
  Preserve the bounded compact skillbook and spellbook tables as fallbacks.
- Enrich observed abilities through an immutable metadata catalog supplied at
  interop composition. Do not couple process-memory parsing to legacy WPF
  metadata managers. Client-observed spell cast lines take precedence over
  configured metadata, while missing values retain the established safe
  defaults.
- Start with capture limits of 64 KiB per block, 4 KiB per string, 4 MiB total,
  4,096 transport reads, and 16 pointer dereferences. Section-specific parsing
  may use tighter limits, and measurement may justify revising the overall
  capture budgets.
- Use a single-owner event loop with reliable commands and coalesced snapshots.
- Schedule the engine from events and deadlines rather than a high-speed fixed
  tick.
- Keep runtime-owned automation disabled until composition supplies one
  immutable configuration containing feature toggles, category order, policies,
  and derived staff catalogs.
- Raise automation cycles from accepted observations and relevant commands,
  evaluate at most one action-producing category per cycle, and require a fresh
  post-action snapshot before another category can act.
- Use snapshot capture cadence as the normal automation observation tick. Do
  not add a second periodic automation timer, and do not publish repeated
  planning state when no observable value changed.
- Schedule snapshot capture independently and choose its cadence through
  measurement.
- Require composition to supply the capture interval explicitly until
  read-only client measurements establish a default. Use one periodic capture
  owner per client so missed periods coalesce instead of creating a backlog,
  and publish only the latest unread result through a bounded channel.
- Retain a bounded rolling timing window with capture and section median, p95,
  and maximum durations, plus capture failure categories and aggregate memory
  read counts and bytes. Keep the timing window capacity configurable and
  bounded.
- Express engine time as elapsed `MacroTimestamp` values derived from an
  injected `TimeProvider`.
- Require a complete in-world snapshot before a macro can enter the running
  state.
- Use `IMacroEngine.Decide` as the pure state-transition contract.
- Name requested external operations `MacroIntent` values so execution remains
  separate from deterministic decisions.
- Name intent types with concise verb-and-noun forms such as
  `EquipWeaponIntent`. Use setter-style prefixes only when they add necessary
  meaning.
- Represent spell queue edits as reliable, identifier-based runtime commands.
- Hydrate complete persisted spell, skill, and flower queues with atomic
  `ReplaceSpellQueueCommand`, `ReplaceSkillQueueCommand`, and
  `ReplaceFlowerQueueCommand` requests. Snapshot enumerable inputs at command
  construction, reset transient queue cursors, and synchronize target rotations
  and flower schedules in the same engine decision.
- Preserve the logical spell queue cursor across insert, move, update, and
  non-current removal operations.
- Use explicit priority, sequential, and round-robin spell queue policies.
- Treat missing and completed sequential entries as skippable, while a
  temporarily unavailable entry blocks later sequential entries.
- Represent panel changes as semantic client intents rather than blocking input
  and polling operations.
- Confirm a client action only from a coherent snapshot whose capture began
  after the intent was issued and whose sequence follows the action baseline.
- Give every client action attempt a unique identifier, scheduled deadline, and
  finite retry budget.
- Publish stable succeeded, timed-out, and cancelled panel transition outcomes.
- Treat the world skill and spell selections as one visible panel for
  confirmation while preserving their distinct targets.
- Model observed character class as a single value with an explicit unknown
  state rather than as legacy flags.
- Represent class-neutral staff metadata separately from a class requirement.
  Map legacy `Class="All"` staff metadata to the neutral form.
- Reject every class-specific staff when character class is unknown.
- Filter staff candidates by class, level, ability level, and current
  availability before ranking them.
- Rank eligible staves by cast lines, keeping an equally good equipped staff,
  then by inventory slot and stable name ordering.
- Do not equip a staff that provides no improvement over the base spell.
- Evaluate staff switch commands from the latest coherent character, inventory,
  and equipment snapshot rather than from command-captured mutable models.
- Sequence staff equipping through the confirmed inventory panel before
  selecting the required inventory display mode and emitting a semantic weapon
  intent. Client slots 1 through 34 use the collapsed mode, while slots 35
  through 59 use the expanded mode and its documented slot origin. Slot 60 is
  synthetic gold state and is never a usable equipment source.
- Represent inventory expansion and collapse as semantic client intents.
  Confirm the toggle from a later coherent snapshot before equipping. Never
  retry an unconfirmed toggle because replaying it could reverse an action the
  client already completed.
- Revalidate the selected class requirement, inventory slot, and staff name
  after panel or inventory-mode changes and before equipment retries.
- Confirm weapon changes only from a later coherent equipment snapshot.
- Publish stable snapshot-unavailable, selection-invalidated,
  panel-unavailable, timed-out, cancelled, and succeeded staff outcomes.
- Resolve immutable staff candidate sets by stable spell queue entry identifier.
  Staff metadata and queue persistence adapters will construct these sets
  outside the deterministic runtime.
- Require character, inventory, and equipment snapshot sections only when a
  spell has configured staff candidates and automatic switching is enabled.
- Carry the staff-adjusted cast lines and duration through confirmed inventory,
  equipment, and spell panel transitions.
- Revalidate the selected queue entry and observed spell before equipping a
  staff and again after the equipment change is confirmed.
- Propagate staff panel, selection, and equipment failures to the spell
  workflow without advancing its queue cursor or leaving a pending action.
- Capture vitals and spellbook entries as immutable snapshot sections. Keep
  observed client action-delay flags separate from runtime-owned cooldown
  deadlines.
- Derive spell readiness from queue target level, observed spell availability,
  current mana policy, client action delay, and local cooldown state.
- Treat an unreachable target level as a stable unavailable result rather than
  retrying it indefinitely.
- Calculate cast duration from configurable zero-line, single-line, and
  multi-line timing plus a positive completion margin.
- Represent spell targets as immutable semantic values. Keep screen-coordinate
  translation and character lookup in the future client action adapter.
- Represent relative and absolute target areas as immutable center, inner
  radius, outer radius, and pixel-offset values. Generate the integer points in
  a Euclidean circle, ordered by increasing distance and then clockwise from
  the upward direction.
- Keep target-area cursors in `MacroState`, keyed separately by stable spell and
  flower queue entry identifiers. Reordering a queue preserves its target
  cursor, while changing or removing a target resets or removes the cursor.
- Resolve an area to one exact tile before emitting `CastSpellIntent`. Advance
  its cursor only when that final cast intent is issued, so panel changes, staff
  changes, planning, vineyard, and mana restoration do not consume a point.
- Keep screen-coordinate pixel offsets distinct from their base point because
  legacy behavior applies window scaling before applying the offset.
- Revalidate the selected queue entry, spell observation, mana, and cooldown
  after a spell-panel transition before emitting `CastSpellIntent`.
- Advance round-robin selection only when the cast intent is issued, not while
  a prerequisite panel transition is pending.
- Treat the calculated cast window as a scheduled, exclusive client action and
  record the spell cooldown from its deterministic completion boundary.
- Require a coherent snapshot captured after the previous cast window before
  another spell can be selected.
- Do not automatically retry a cast intent because replaying an uncertain cast
  can spend mana or affect a target twice.
- Represent active skills as an ordered, identifier-based queue with a stable
  round-robin cursor. Select at most one ready skill action per engine
  decision.
- Build immutable skillbook snapshot sections from observed skill state and
  metadata outside the runtime. Keep pane selection and raw cooldown parsing in
  interop.
- Treat minimum skill and spell health as an exclusive boundary and maximum
  health as an inclusive boundary, matching the existing metadata behavior.
- Require vitals for planning when mana checks are enabled or a queued skill or
  spell has a health condition. Do not require an unrelated snapshot section
  when neither rule applies.
- Combine observed skill action-delay state with runtime-owned monotonic
  cooldown deadlines. A skill becomes ready at the exact local deadline.
- Model space-bar and individual-slot assails as policy choices that resolve to
  semantic action kinds. Keep key and mouse translation in the future client
  action adapter.
- Derive disarm requirements from explicit skill metadata and the
  disarm-for-assails policy before any client action is requested.
- Capture weapon and shield observations separately. A disarm prerequisite is
  complete only when a later coherent equipment snapshot shows both hands
  empty.
- Represent disarming as `DisarmIntent`, independent of the active client
  panel. Give it a finite retry budget and propagate timeout or missing
  equipment state to the waiting skill workflow.
- Represent individual skill activation as `UseSkillIntent` and space-bar
  assailing as `AssailIntent`. Client adapters own the corresponding input
  translation.
- Revalidate the selected queue entry, observed skill, health, mana, cooldown,
  action kind, and disarm requirement after every prerequisite confirmation.
- Advance the skill queue only when the final skill or assail intent is issued.
  Panel and disarm attempts do not consume its selection.
- Treat skill and assail activation as a configurable, scheduled action window.
  Record local cooldowns from its deterministic completion boundary and require
  a snapshot captured after that boundary before planning another skill.
- Do not automatically retry a skill or assail intent because replaying an
  uncertain activation can trigger the action twice.
- Represent delayed dialog cleanup as scheduled `DialogCloseDue` events and
  `CancelDialogIntent`, not callbacks. A newer dialog-opening spell or skill
  supersedes an older close event through its recorded due time.
- Defer a due dialog close until the active bounded client action ends. Dialog
  cancellation itself is a single-attempt bounded action and is cancelled by
  pause, stop, or logout.
- Treat the coherent user-chatting observation as an automation gate. Continue
  accepting snapshots while the user types, but do not select a new automatic
  action until a later snapshot shows that typing has ended.
- Apply map-change policy before coordinate-change policy when both change in
  one accepted snapshot. Continue, pause, and stop are explicit configuration
  choices. An interruption accepts the new snapshot, cancels in-flight work,
  and never terminates the client process.
- Capture a known active panel only when an automatic category starts bounded
  work. After that work finishes, use a later fresh snapshot to restore the
  original panel before selecting another automatic action.
- Reuse bounded panel transition attempts for preservation. Publish explicit
  succeeded, timed-out, issue-failed, and cancelled outcomes, and cancel
  preservation on pause, stop, logout, or another lifecycle interruption.
- Compose persisted queues and current application settings into one atomic
  runtime setup command before lifecycle changes.
- Build staff catalogs per client from the observed character class. Treat
  `Class="All"` as neutral, include multi-class legacy metadata only when it
  includes the observed class, and keep the exact class on the runtime
  candidate so later snapshots revalidate eligibility.
- Preserve the cooldown-skip setting as an explicit spell policy. Map legacy
  movement `ForceQuit` to runtime stop, because the deterministic engine does
  not emit a process-termination intent.
- Route toolbar, hotkey, and stop-all lifecycle changes through the same
  Community Toolkit command surface. Project running and paused state from the
  runtime, which is the only automation lifecycle authority.
- Keep the transitional macro editor as a Community Toolkit observable
  `PlayerMacroConfiguration` with no execution state, locks, threads, timers, or
  input authority. Give each process one DI-owned configuration and mutate it on
  the UI thread.
- Snapshot the editable configuration in memory and import it through the tested
  legacy configuration reader before every start or resume. Send the complete
  atomic runtime setup before the lifecycle command.
- Allow skill toggles and complete spell or flower queue edits while running.
  Recompose the full editor state after each completed mutation and submit it
  as one reliable runtime command so queues and policy cannot be observed from
  different editor revisions.
- Represent flower queues as immutable entries with stable identifiers,
  monotonic interval schedules, deterministic rotation, and interval or
  character-mana conditions. When both conditions are configured, either can
  make the entry ready.
- Keep flower target selection separate from spell execution. Target planning
  consumes immutable observations from all clients and never reaches into a
  global macro or player manager.
- Require coherent map locations before selecting character or tile flower
  targets. Character targets must be logged in, on the same exact map, and
  within the configured X and Y bounds.
- When alternate characters are prioritized, select the eligible character
  waiting longest since its last flower and rotate fairly among configured
  character entries. Exclude the source client, stopped macros, logged-out
  clients, and out-of-range clients.
- Publish an immutable cross-client `ClientRosterSnapshot` through a coalesced
  latest-value mailbox. Accept only monotonic roster sequences whose capture
  time is not in the engine's future. Flower planning and named-character spell
  targeting share this observation boundary.
- Represent flower queue edits as reliable, identifier-based commands and
  synchronize interval schedules from the engine's monotonic clock.
- Select mana restoration, vineyard, and planting as deterministic flower
  actions, then execute them through the shared spell, panel, staff, cooldown,
  deadline, and cancellation workflow.
- Give prioritized waiting characters precedence over vineyard. Otherwise,
  attempt vineyard before the configured queue target when the spell is ready.
- Allow mana restoration from an explicit threshold or when the selected plant
  needs more mana. Replan from a fresh source snapshot before the following
  plant attempt.
- Revalidate the selected flower target and spell after every confirmed panel
  or staff prerequisite. Invalidate a moved or otherwise ineligible target
  before issuing a cast intent.
- Advance the flower queue and record its interval schedule only when the final
  plant `CastSpellIntent` is issued. Vineyard, restoration, and prerequisite
  actions do not consume the selected target.
- Do not automatically retry a flower cast intent because replaying an
  uncertain cast can spend mana or affect a target twice.
- Isolate macro configuration in `SleepHunter.Persistence`, which depends only
  on `SleepHunter.Runtime`. Do not serialize transient `MacroState`.
- Remove the legacy `MacroState` executor, its lock-protected queues, deferred
  dispatcher, execution status flags, and 16 ms flower worker once the
  deterministic runtime owns every lifecycle command and observation.
- Write new macro configurations as schema version 1 JSON using the `.sh4x`
  extension. Treat XML `.sh4` as an import-only legacy format.
- Keep shipped skills, spells, staves, themes, and the configurable client
  layout in XML. Their stable, attribute-heavy metadata schemas and editor
  support are separate from user-owned macro persistence and do not benefit
  from being folded into the `.sh4x` migration.
- Preserve an unresolved legacy default spell rotation so application settings
  can supply the fallback. Map legacy singular and round-robin modes to their
  deterministic equivalents, and map legacy none to priority order with a
  migration warning.
- Reconstruct stable queue entry identifiers from legacy order and report
  duplicate, unusable, normalized, or behavior-modernized legacy data through
  structured migration warnings.
- Recover a nonzero legacy flower interval when the saved `HasInterval` marker
  contradicts it, and report that repair as a migration warning.
- Build and test the runtime before beginning broad MVVM conversion.
- Use CommunityToolkit.Mvvm for new WPF ViewModels and commands where its
  focused components reduce boilerplate.
- Pin CommunityToolkit.Mvvm 8.4.2 centrally and keep it scoped to the WPF
  application.
- Host each client through one Interop-owned runtime host that serializes
  snapshot publication, intent execution, issuance feedback, and awaited
  disposal without depending on WPF.
- Publish capture diagnostics from each runtime host through a bounded,
  latest-value observation stream that includes failures and rolling timing
  statistics. Project that stream into bindable WPF state without adding a
  polling loop.
- Attach one shadow runtime to each discovered Dark Ages client before UI
  cutover. Use only query and virtual-memory-read process rights, accept only an
  atomic replacement of all configured queues at the shadow host boundary,
  reject lifecycle, incremental queue, and action-producing commands, and await
  host disposal during client removal and application shutdown.
- Promote attached hosts from the shadow boundary to the active command
  boundary before toolbar cutover. Give every attached host the same macro
  clock, enrich ability snapshots from application metadata, and publish only
  changed cross-client roster projections.
- Load macro files for the shadow runtime through a CommunityToolkit-based
  configuration view model. Apply all persisted queues with one aggregate
  command, preserve the previous accepted configuration when loading fails, and
  expose structured migration warnings and errors as observable state. During
  the transition, the legacy UI and shadow runtime read the same file through
  their respective adapters.
- Remove the transition configuration view model once start and resume compose
  the complete typed editor configuration. File loading then updates only the
  editor, preventing duplicate idle-runtime queue synchronization.
- Make the client card the first vertical MVVM slice. Prefer coherent runtime
  character, presence, location, and vitals observations, fall back per section
  when the runtime observation is unavailable, and leave automation authority
  on the legacy player model until later slices are ready.
- Design a scripting-compatible intent boundary without adding scripting now.
- Treat MoonSharp as a future optional adapter, not a runtime dependency.
- Keep patching isolated from automation decisions.

## Deferred Decisions

The following decisions should be made when evidence or implementation requires
them:

- Exact snapshot cadence and adaptive observation policy.
- Whether snapshot sections need independent capture rates.
- Final runtime type names.
- Exact WPF hosting and dependency-injection packages.
- Whether game metadata needs a separate assembly.
- Timing of patcher extraction.
- Whether safe skill or assail actions may interleave with an active spell cast
  window while preserving the single-writer pending-action invariant.
- Legacy macro-state compatibility and migration details.
- Lua script trust model and capabilities.
- MoonSharp package and version.
- Script lifecycle, debugging, and distribution experience.

Update this document whenever a deferred decision becomes settled or a pull
request establishes a new architectural constraint.
