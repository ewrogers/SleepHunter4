# Installation

## Requirements

- A 32-bit [Dark Ages](https://www.darkages.com) client compatible with the
  configured client layout
- .NET 10.0 Desktop Runtime
    - Windows arm64 - [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.10-windows-arm64-installer)
    - Windows x64 - [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.10-windows-x64-installer)
    - Windows x86 - [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.10-windows-x86-installer)
- A [Windows version supported by .NET 10](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) (64-bit)

## Installation

1. Download the latest version from the [Releases](https://github.com/ewrogers/SleepHunter4/releases) page.
2. Extract the `SleepHunter-4.x.x.zip` file to a folder of your choice.
3. Run `SleepHunter.exe` to start the application.
4. Configure your game client settings in the `Settings->GameClient` section.
5. Read the rest of this documentation to learn how to use the application.

It is very important that you extract **all** of the files from the `SleepHunter-4.x.x.zip` file.

## Auto-Update

As of version **4.1.0**, SleepHunter will automatically check for updates when it starts.
It checks the same GitHub releases page that you downloaded the application from.

If an update is available, you will be prompted to download and install it.
SleepHunter will automatically close and restart after the update is applied.

You can also manually check for updates by clicking the `Check for Updates` button in the `Settings->Updates` section.

**NOTE:** Your `Settings.xml` file will **not** be overwritten when you update SleepHunter.

## Manual Update

If you are unable to update SleepHunter automatically, you can manually download and install the latest version.
Follow the same steps as the [Installation](#installation) section, but replace the existing files with the new ones.

In most cases, you do not have to overwrite the `Settings.xml` file.

## Uninstall

To uninstall SleepHunter, simply delete the folder that you extracted the application to.
There are no registry entries or other files that need to be removed.

