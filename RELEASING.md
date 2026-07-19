# Releasing SleepHunter

SleepHunter releases are built and published by GitHub Actions from version tags.

## Prepare a release

1. Update `Version`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion` in `SleepHunter/SleepHunter.csproj`.
2. If the updater changed, update the same version properties in `SleepHunter.Updater/SleepHunter.Updater.csproj`.
3. Add a dated section for the SleepHunter version to `CHANGELOG.md`.
4. Merge the release changes into `main` and confirm the CI workflow passes.

## Publish a release

Create and push an annotated tag matching the SleepHunter version:

```powershell
git tag -a v4.11.1 -m "SleepHunter 4.11.1"
git push origin v4.11.1
```

The Release workflow validates the tag, project metadata, updater metadata, and changelog. It then publishes both applications for `win-x64`, verifies the auto-update archive contract, and creates the GitHub release with a single `SleepHunter-X.Y.Z.zip` asset.

The release archive intentionally has a flat layout containing:

- `SleepHunter.exe`
- `Updater.exe`
- `Versions.xml`
- `Themes.xml`
- `Skills.xml`
- `Spells.xml`
- `Staves.xml`

`Settings.xml` is intentionally excluded. New installations create it with defaults, and the updater preserves an existing settings file.
