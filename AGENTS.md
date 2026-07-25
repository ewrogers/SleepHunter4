# SleepHunter Repository Guidance

## Scope

These instructions apply to the entire repository. Follow any more specific
`AGENTS.md` file if one is added below a subdirectory in the future.

SleepHunter is a Windows WPF application for automating and observing the Dark
Ages game client. Changes can affect live process memory, input, client patches,
and user automation, so favor correctness, compatibility, and bounded reads over
speculative behavior.

## Mandatory Writing Rules

- Never use the Unicode em dash character.
- Never use emojis or pictographic symbols.
- Apply these rules to source code, comments, tests, documentation, changelogs,
  commit messages, pull request text, issue text, and user-facing copy.
- Use commas, colons, parentheses, or a standard hyphen when punctuation is
  needed instead of an em dash.
- Do not rewrite unrelated existing text solely to remove older violations.
  Any text that is added or materially edited must follow these rules.

## Repository Layout

- `SleepHunter/` contains the main .NET 9 WPF application.
- `SleepHunter.Updater/` contains the Windows updater.
- `SleepHunter.Tests/` contains NUnit tests.
- `data/` contains runtime XML data, including client versions, themes, skills,
  spells, and staves.
- `docs/` contains the mdBook documentation source.
- `CHANGELOG.md` is the authoritative changelog. Documentation deployment copies
  it into the generated documentation output.
- `.github/workflows/` defines build, test, documentation, and release behavior.

## Working Practices

1. Inspect the relevant code, tests, configuration, and current Git status before
   editing.
2. Preserve unrelated user changes and avoid broad cleanup outside the requested
   scope.
3. Make the smallest coherent change that solves the problem.
4. Preserve compatibility paths unless the task explicitly removes support for
   them.
5. Add or update tests for behavior that can be validated without a live client.
6. Update `CHANGELOG.md` for user-visible changes.
7. Report checks that were run and any live-client behavior that still needs
   manual verification.

Do not commit build output from `bin/` or `obj/`. Do not modify generated
documentation output when the source belongs in `docs/src/` or `CHANGELOG.md`.

## Build and Test

Run commands from the repository root.

```powershell
dotnet restore SleepHunter.sln
dotnet build SleepHunter.sln --configuration Release --no-restore
dotnet test SleepHunter.sln --configuration Release --no-build
```

For a focused test run, use an NUnit-compatible filter and still run the full
suite before handing off a substantial change.

```powershell
dotnet test SleepHunter.Tests/SleepHunter.Tests.csproj `
    --configuration Release `
    --no-build `
    --filter "FullyQualifiedName~TestClassOrMethod"
```

The WPF projects require Windows and the .NET 9 SDK. When a running SleepHunter
process locks Debug output, use a Release build for verification and do not stop
the user's process without permission.

Documentation tests run in CI with mdBook:

```powershell
Set-Location docs
mdbook test
```

## C# and XAML Style

- Treat `.editorconfig` as authoritative.
- Use four spaces for C# and two spaces for XML project and configuration files.
- Keep block-scoped namespaces and braces consistent with the surrounding code.
- Sort `System` using directives first.
- Prefer `var` where the existing style and `.editorconfig` permit it.
- Use `SetProperty` and related notification helpers for bindable model state.
- Keep UI behavior in bindings, templates, converters, or view models when
  practical. Avoid code-behind changes that duplicate existing binding logic.
- Avoid unrelated formatting changes in XAML because they make visual reviews
  harder.
- Follow the nullable configuration of the project being edited. The test
  project enables nullable reference types, while the main application currently
  does not.

## Memory Mapping and Live Client Safety

- Treat `data/Versions.xml` as the source of client-specific mapping
  configuration.
- Do not guess addresses, offsets, pointer depth, field widths, signedness, or
  collection capacities. Base changes on client documentation, disassembly,
  packet behavior, or a reproducible live-memory observation.
- State the exact client version and executable variant that supports a mapping.
  Do not silently apply one client's layout to another profile.
- Validate pointers, counts, lengths, indices, and generation or ownership state
  before dereferencing mutable client memory.
- Prefer bounded snapshots and verify collection roots or generation values again
  after reading when the client can mutate data concurrently.
- Reject partial or incoherent snapshots. Clear stale pane-only state when its
  source disappears.
- Preserve compact or legacy mapping fallbacks when richer pane data is
  unavailable, unless removal is explicitly requested and tested.
- Keep raw memory reads in the process and mapping layers. Expose typed,
  domain-level values from models.
- Add parser or snapshot tests for every documented field layout that can be
  represented with deterministic byte arrays.
- For pointer fixes, document why the previous root was wrong and how the new
  root was verified.

## Client Patching and Input Safety

- Treat runtime patch bytes, signatures, offsets, and calling conventions as
  high-risk changes.
- Require an exact signature or equivalent guard before writing to another
  process.
- Fail safely when a patch cannot be verified. Do not launch or leave a client in
  a partially patched state.
- Keep version-specific patches isolated and add tests for signature matching,
  byte generation, and failure behavior.
- Do not terminate, suspend, resume, or modify a user's running client unless the
  requested task requires it and the action is clearly communicated.

## Tests

- Use NUnit in `SleepHunter.Tests/`.
- Name tests as behavior statements, following the existing `Should...` pattern.
- Use `Assert.Multiple` when several properties describe one parsed snapshot or
  result.
- Cover invalid pointers, empty data, boundary counts, stale state, and timer
  wraparound where relevant.
- Keep tests deterministic. Do not require a live Dark Ages process in the unit
  test suite.
- Manual client checks complement automated tests but do not replace them.

## Documentation and Release Notes

- Keep public documentation focused on observable behavior and configuration.
- Put technical memory-layout details near the relevant code or in focused
  documentation rather than overloading user setup pages.
- Add user-visible changes under the appropriate section in the root
  `CHANGELOG.md`.
- Keep release versions synchronized across project metadata, tags, and the
  changelog as required by `.github/workflows/release.yml`.
- Do not edit generated files under `docs/book/`.

## Git and Pull Requests

- Use a focused branch and concise commits.
- Stage only files that belong to the requested change.
- Do not rewrite or discard user work with destructive Git commands.
- Pull request descriptions must explain what changed, why it changed, user
  impact, root cause for fixes, and validation performed.
- Call out anything that still requires testing against the live 7.41 client.
