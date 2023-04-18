
# SleepHunter
<img src="SleepHunter/SleepHunter.png" width=32 height=32/> <img src="SleepHunter.Updater/SleepHunter-Updater.png" width=32 height=32/>
Dark Ages Automation Tool + Updater

---

<img src="docs/src/screenshots/SleepHunter.png"/>

## Requirements ✅

- [Dark Ages](https://www.darkages.com) Client 7.41 (current latest)
- .NET 7.0 Runtime
    - Windows arm64 - [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.5-windows-arm64-installer)
    - Windows x64 - [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.5-windows-x64-installer)
    - Windows x86 - [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.5-windows-x86-installer)
- Windows 7, 10, 11 (64-bit)

## Installation 💾

1. Download the [latest release](https://github.com/ewrogers/SleepHunter4/releases/)
2. Extract all files to `C:\SleepHunter` (or your choosing)
3. Open `SleepHunter.exe`
4. Configure your DA installation path in `Settings->Game Client` (if different)
5. Profit!

## Documentation 📚

The documentation for SleepHunter is located in the [docs](./docs) folder.
It is written in [Markdown](https://www.markdownguide.org/) and can be viewed in any text editor.

There is a GitHub action that will automatically build the documentation into a static website and publish it to GitHub Pages.

[View Documentation](https://ewrogers.github.io/SleepHunter4/)

## Auto-Update 🔄

Starting with version 4.1.0, the long awaited auto-update functionality is now working!
It pulls from the [latest release](https://github.com/ewrogers/SleepHunter4/releases) section.

This means you can update from within the SleepHunter application itself by going to `Settings->Updates`.
If there is a new version available, you can update to it which will download, install, and restart SleepHunter.

**NOTE**: Your user settings **will be preserved**, but all other existing data files will be overwritten.

## Contributing 👨🏻‍💻

I am always accepting of pull requests (PRs) against this repository for additional features, bug fixes, and enhancements.
Now that Auto-Update is functional, it should be much easier to distribute these changes to users of the application.

It is recommended that you use [Visual Studio 2022+](https://visualstudio.microsoft.com/vs/0) for developing on Windows.
I am not sure of WPF support within other IDEs.

Unfortunately this repository does not have *any* unit tests, so you will have to test for regressions manually.
Please be mindful of the users of this application, and thoroughly test any functionality for breaking changes.

## Packaging 📦

To package and deploy the application and updater binaries as neat, single-file executables, use the following command:

```powershell
cd SleepHunter
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained false

```

You will get similar output:

```powershell
MSBuild version 17.5.1+f6fdcf537 for .NET
  Determining projects to restore...
  Restored C:\Users\Erik\Projects\SleepHunter4\SleepHunter.Updater\SleepHunter.Updater.csproj (in 230 ms).
  Restored C:\Users\Erik\Projects\SleepHunter4\SleepHunter\SleepHunter.csproj (in 230 ms).
  SleepHunter.Updater -> C:\Users\Erik\Projects\SleepHunter4\SleepHunter.Updater\bin\Release\net7.0-windows\win-x64\Upd
  ater.dll
  SleepHunter.Updater -> C:\Users\Erik\Projects\SleepHunter4\SleepHunter.Updater\bin\Release\net7.0-windows\win-x64\pub
  lish\
  SleepHunter -> C:\Users\Erik\Projects\SleepHunter4\SleepHunter\bin\Release\net7.0-windows\win-x64\SleepHunter.dll
  SleepHunter -> C:\Users\Erik\Projects\SleepHunter4\SleepHunter\bin\Release\net7.0-windows\win-x64\publish\
```

You should then see the binaries in `$PROJECT_ROOT/bin/Release/.net7.0-windows/win-x64/publish`.

Unfortunately, it seems publishing through VS 2022 does not respect the `PublishSingleFile` property, even when specified in the `.csproj` file.
