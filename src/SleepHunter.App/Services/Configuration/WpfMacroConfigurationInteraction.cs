using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SleepHunter.Extensions;
using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Configuration
{
    public sealed class WpfMacroConfigurationInteraction :
        IMacroConfigurationInteraction
    {
        private static readonly string CurrentExtension =
            MacroConfigurationSerializer.CurrentFileExtension
                .TrimStart('.');
        private static readonly string LegacyExtension =
            MacroConfigurationSerializer.LegacyFileExtension
                .TrimStart('.');
        private static readonly string LoadFilter =
            $"SleepHunter Macro Configurations (*.{CurrentExtension})|*.{CurrentExtension}|" +
            $"SleepHunter v4 Macro Files (*.{LegacyExtension})|*.{LegacyExtension}";
        private static readonly string SaveFilter =
            $"SleepHunter Macro Configurations (*.{CurrentExtension})|*.{CurrentExtension}";

        private readonly Window owner;

        public WpfMacroConfigurationInteraction(Window owner)
        {
            this.owner = owner ??
                throw new ArgumentNullException(nameof(owner));
        }

        public string SelectLoadFile(string characterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

            var dialog = new OpenFileDialog
            {
                Title = "Load Macro Configuration",
                Filter = LoadFilter,
                DefaultExt = CurrentExtension,
                FileName = $"{characterName}.{CurrentExtension}",
                InitialDirectory = Path.Combine(
                    Environment.CurrentDirectory,
                    "saves"),
                Multiselect = false,
                CheckPathExists = true,
                CheckFileExists = true
            };
            return dialog.ShowDialog(owner).GetValueOrDefault()
                ? dialog.FileName
                : null;
        }

        public string SelectSaveFile(string characterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

            var dialog = new SaveFileDialog
            {
                Title = "Save Macro Configuration",
                Filter = SaveFilter,
                DefaultExt = CurrentExtension,
                FileName = $"{characterName}.{CurrentExtension}",
                InitialDirectory = Path.Combine(
                    Environment.CurrentDirectory,
                    "saves"),
                OverwritePrompt = true,
                AddExtension = true,
                CheckPathExists = true,
                ValidateNames = true
            };
            return dialog.ShowDialog(owner).GetValueOrDefault()
                ? dialog.FileName
                : null;
        }

        public void ShowMessage(
            string title,
            string message,
            string detail) =>
            owner.ShowMessageBox(
                title,
                message,
                detail);
    }
}
