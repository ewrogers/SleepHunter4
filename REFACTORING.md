# SleepHunter Runtime Refactor

## Status

This is the living engineering plan for the SleepHunter runtime and application
refactor. Work begins on the `codex/runtime-refactor` branch and will be delivered
through a series of focused pull requests.

The next release is expected to contain the completed refactor. There is no
planned interim release containing repairs to the legacy macro engine. Every
pull request must still build successfully and include automated validation for
the behavior it introduces.

## Executive Decision

SleepHunter will receive a new unit-tested runtime built beside the legacy
implementation. The new runtime will be designed as if the automation engine
were being built today:

- Deterministic state transitions.
- A single owner for mutable macro state.
- Immutable, sequenced client snapshots.
- Channels for reliable commands and coalesced observations.
- Explicit actions, pending transitions, deadlines, and failure outcomes.
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

The refactor starts with two new production projects and their matching test
projects:

```text
SleepHunter.Runtime
SleepHunter.Interop

SleepHunter.Runtime.Tests
SleepHunter.Interop.Tests
```

The existing `SleepHunter` WPF application and `SleepHunter.Updater` remain in
place during development.

Additional assemblies will be created only when their active implementation
justifies a dependency boundary. Likely later candidates are:

```text
SleepHunter.Patching
SleepHunter.Scripting.MoonSharp
```

Avoid generic dumping-ground projects such as `Common`, `Shared`, or
`Infrastructure`.

### SleepHunter.Runtime

`SleepHunter.Runtime` targets plain `net9.0` and has no dependency on another
SleepHunter project. It contains both the pure engine and its asynchronous host,
organized by responsibility:

```text
Engine/
Sessions/
Scheduling/
Observations/
Actions/
```

The engine area contains deterministic state, events, decisions, and effects.
The session area contains channels, scheduling, lifecycle, and effect dispatch.
Keeping them in one assembly avoids premature project fragmentation while tests
and namespace rules preserve the logical boundary.

`SleepHunter.Runtime` must not reference:

- WPF or a UI dispatcher.
- Win32 or process APIs.
- Client-version memory mappings.
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
ClientVersions/
Win32/
```

It discovers and attaches to client processes, performs bounded memory reads,
builds immutable runtime snapshots, sends guarded window input, and translates
between raw client data and runtime observation or action types.

`SleepHunter.Interop` targets Windows and depends on `SleepHunter.Runtime`.

### Dependency Direction

```text
SleepHunter
  -> SleepHunter.Runtime
  -> SleepHunter.Interop

SleepHunter.Interop
  -> SleepHunter.Runtime

SleepHunter.Runtime
  -> no SleepHunter project dependencies
```

If patching is extracted later:

```text
SleepHunter.Patching
  -> SleepHunter.Interop
```

New projects must never reference the legacy WPF project. During integration,
the legacy application may reference the new projects, keeping dependency flow
inward.

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

`SleepHunter` and `SleepHunter.Updater` each reference:

- `Microsoft.CSharp` 4.7.0.
- `System.Data.DataSetExtensions` 4.5.0.

No source usage of `dynamic`, the C# runtime binder, `System.Data`, `DataSet`,
`DataTable`, or the DataSet extension APIs was found. Both packages are strong
removal candidates because the projects target .NET 9 and the relevant
framework surface is supplied by the platform. Their removal must still be
proved by restoring, building, testing, and publishing both applications.

`SleepHunter.Tests` directly references:

- `Microsoft.NET.Test.Sdk` 18.0.1.
- `NUnit` 4.4.0.
- `NUnit.Analyzers` 4.11.2.
- `NUnit3TestAdapter` 5.2.0.

These packages have active and distinct test execution, framework, analysis,
and adapter responsibilities. Newer versions were available at audit time.
They should be updated together in a focused change after compatibility is
verified, rather than mixed into engine behavior work.

The transitive test packages, including test-platform telemetry, coverage,
Application Insights, and Newtonsoft.Json components, arrive through the test
SDK and adapter. They are not application runtime dependencies and should not
be referenced directly unless a future feature independently requires them.

`Microsoft.NET.ILLink.Tasks` is automatically supplied by the SDK for the
single-file publish configuration. It is not an explicit application package.

The current NuGet sources reported no known vulnerable direct or transitive
packages and no deprecated direct packages at audit time.

The only current project reference is from `SleepHunter.Tests` to
`SleepHunter`. The new project graph will replace that broad testing boundary
with focused runtime and interop test projects.

A tracked generated file,
`SleepHunter.Updater/SleepHunter.Updater_0ypjaz0w_wpftmp.csproj`, contains stale
machine-specific .NET 7 reference paths and an apparent
`Microsoft.Windows.Compatibility` dependency. It is not part of the solution or
the real updater dependency graph. Remove it and add an ignore rule for WPF
temporary project files during repository scaffolding.

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
- `MacroDecision`: a deterministic state transition and its resulting effects.
- `MacroEffect`: a requested external operation.
- `PendingAction`: an issued operation awaiting confirmation or failure.
- `MacroViewSnapshot`: immutable state published to UI and scripting consumers.

Final names may change as the model becomes concrete. Their responsibilities
must remain distinct.

### Pure Engine

The core decision function is conceptually:

```text
Step(current state, input event, current time)
    -> new state
    -> zero or more internal events
    -> zero or one external client action
    -> next deadline
```

Given the same state, event, snapshot, and time, it must produce the same
result. It may not read wall-clock time, client memory, settings files, or WPF
state.

The engine should emit at most one exclusive external client action before
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
4. Dispatches any permitted client action.
5. Publishes an immutable view snapshot when observable state changes.
6. Calculates the next deadline and awaits the next input.

No engine path should use `Thread.Sleep`, synchronous task waiting, or an
unbounded polling loop.

## Snapshot and Observation Strategy

Snapshot capture and engine execution are independently scheduled. The engine
must not call `ReadProcessMemory` directly.

Every snapshot contains:

- A sequence number.
- Capture start and completion timestamps.
- Client identity and version.
- Validity or quality information.
- Explicit freshness for independently captured sections when applicable.

A newer snapshot must be required to confirm an action. For example, equipment
data captured before an equip action cannot confirm that action.

Snapshot capture follows these rules:

- Only one capture may run per client at a time.
- Slow captures cause later observations to be skipped or coalesced, never
  queued into a backlog.
- Reads remain bounded and version-specific.
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

1. The engine emits an action.
2. Interop sends the input and returns an issuance result.
3. The session records a pending action with an expected condition and
   deadline.
4. A newer snapshot confirms success, reports an invalid condition, or reaches
   the deadline.
5. The engine chooses the next deterministic transition.

Pending actions contain enough information to diagnose and test behavior:

- Action identifier and kind.
- Issued timestamp.
- Expected confirmation condition.
- Deadline.
- Attempt count.
- Failure and cancellation outcome.

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
8. Remaining features such as water and beds.
9. Macro-state persistence and compatibility migration.

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
Tests assert the exact actions requested and the states published over a
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
- No actions emitted after shutdown.

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

The exact package version will be selected and centrally pinned when WPF
implementation begins.

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

- Add state, command, event, effect, decision, and snapshot primitives.
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
- Add version-specific parser tests.
- Add capture timing instrumentation.
- Perform read-only measurement against the supported client.
- Select initial observation cadence based on measurements.

### PR 6: Interop Action Execution

- Translate runtime actions into client input.
- Preserve guards and version-specific behavior.
- Test action translation and cancellation without live input.

### PR 7: Persistence and Compatibility

- Define versioned runtime state persistence.
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
- Audit and minimize existing dependencies before establishing the new package
  graph.
- Keep pure decisions and channel-driven hosting in one runtime assembly
  initially.
- Use a single-owner event loop with reliable commands and coalesced snapshots.
- Schedule the engine from events and deadlines rather than a high-speed fixed
  tick.
- Schedule snapshot capture independently and choose its cadence through
  measurement.
- Build and test the runtime before beginning broad MVVM conversion.
- Use CommunityToolkit.Mvvm for new WPF ViewModels and commands where its
  focused components reduce boilerplate.
- Design a scripting-compatible intent boundary without adding scripting now.
- Treat MoonSharp as a future optional adapter, not a runtime dependency.
- Keep patching isolated from automation decisions.

## Deferred Decisions

The following decisions should be made when evidence or implementation requires
them:

- Exact snapshot cadence and adaptive observation policy.
- Whether snapshot sections need independent capture rates.
- Final runtime type names.
- Exact CommunityToolkit.Mvvm version.
- Exact WPF hosting and dependency-injection packages.
- Whether game metadata and persistence need separate assemblies.
- Timing of patcher extraction.
- Legacy macro-state compatibility and migration details.
- Lua script trust model and capabilities.
- MoonSharp package and version.
- Script lifecycle, debugging, and distribution experience.

Update this document whenever a deferred decision becomes settled or a pull
request establishes a new architectural constraint.
