using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Macro;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Services.Logging;

namespace SleepHunter.Services.Configuration
{
    public sealed class MacroConfigurationPersistenceService :
        IMacroConfigurationPersistenceService
    {
        private readonly string autosaveDirectory;
        private readonly IHotkeyRegistrationService hotkeys;
        private readonly ILogger logger;
        private readonly IPlayerMacroConfigurationMapper mapper;
        private readonly IMacroConfigurationReader reader;
        private readonly IMacroConfigurationWriter writer;

        public MacroConfigurationPersistenceService(
            IMacroConfigurationReader reader,
            IMacroConfigurationWriter writer,
            IPlayerMacroConfigurationMapper mapper,
            IHotkeyRegistrationService hotkeys,
            ILogger logger,
            string applicationDirectory)
        {
            this.reader = reader ??
                throw new ArgumentNullException(nameof(reader));
            this.writer = writer ??
                throw new ArgumentNullException(nameof(writer));
            this.mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
            this.hotkeys = hotkeys ??
                throw new ArgumentNullException(nameof(hotkeys));
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            ArgumentException.ThrowIfNullOrWhiteSpace(
                applicationDirectory);
            autosaveDirectory = Path.Combine(
                Path.GetFullPath(applicationDirectory),
                "autosave");
        }

        public async Task<MacroConfigurationApplyResult> LoadAsync(
            PlayerMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var fullPath = Path.GetFullPath(filePath);
            var loaded = await ReadAsync(
                configuration,
                fullPath,
                cancellationToken);
            return Apply(configuration, loaded);
        }

        private async Task<MacroConfigurationLoadResult> ReadAsync(
            PlayerMacroConfiguration configuration,
            string fullPath,
            CancellationToken cancellationToken)
        {
            logger.LogInfo(
                $"Loading {configuration.Client.Name} macro configuration from {fullPath}...");
            var loaded = await reader
                .LoadAsync(fullPath, cancellationToken);
            logger.LogInfo("Deserialized successfully");
            return loaded;
        }

        private MacroConfigurationApplyResult Apply(
            PlayerMacroConfiguration configuration,
            MacroConfigurationLoadResult loaded)
        {
            var previousHotkey = configuration.Client.Hotkey;
            mapper.Apply(configuration, loaded);

            if (previousHotkey is not null)
                hotkeys.Unregister(previousHotkey);

            var registrationFailed = false;
            if (configuration.Client.Hotkey is { } importedHotkey &&
                !hotkeys.Register(importedHotkey))
            {
                logger.LogWarn(
                    $"Unable to register the imported macro hotkey for {configuration.Client.Name}.");
                configuration.Client.Hotkey = null;
                registrationFailed = true;
            }

            foreach (var warning in loaded.Warnings)
            {
                logger.LogWarn(
                    $"Macro configuration migration warning {warning.Code}: {warning.Message}");
            }

            logger.LogInfo(
                $"Updated {configuration.Client.Name} macro configuration from {loaded.Format} data");
            return new MacroConfigurationApplyResult(
                loaded,
                registrationFailed);
        }

        public async Task SaveAsync(
            PlayerMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var fullPath = Path.GetFullPath(filePath);
            logger.LogInfo(
                $"Saving {configuration.Client.Name} macro configuration into {fullPath}...");
            var snapshot = mapper.CreateSnapshot(configuration);
            await writer.SaveAsync(
                snapshot,
                fullPath,
                cancellationToken);
            logger.LogInfo("Serialized successfully");
        }

        public async Task<MacroConfigurationAutoLoadResult>
            AutoLoadAsync(
            PlayerMacroConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var currentPath = GetAutosavePath(
                configuration,
                MacroConfigurationSerializer.CurrentFileExtension);
            var legacyPath = GetAutosavePath(
                configuration,
                MacroConfigurationSerializer.LegacyFileExtension);
            var sourcePath = File.Exists(currentPath)
                ? currentPath
                : legacyPath;
            if (!File.Exists(sourcePath))
            {
                logger.LogInfo(
                    $"Auto-save file does not exist: {currentPath}");
                return null;
            }

            MacroConfigurationLoadResult loaded;
            try
            {
                loaded = await ReadAsync(
                    configuration,
                    sourcePath,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                DeleteBrokenAutosave(sourcePath);
                throw;
            }

            var applied = Apply(configuration, loaded);
            var migrated = false;
            if (applied.Loaded.Format ==
                MacroConfigurationFormat.LegacyV4)
            {
                try
                {
                    await SaveAsync(
                        configuration,
                        currentPath,
                        cancellationToken);
                    migrated = true;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogException(exception);
                    logger.LogWarn(
                        $"Unable to migrate legacy autosave file: {sourcePath}");
                }
            }

            return new MacroConfigurationAutoLoadResult(
                applied,
                sourcePath,
                migrated);
        }

        public Task AutoSaveAsync(
            PlayerMacroConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return SaveAsync(
                configuration,
                GetAutosavePath(
                    configuration,
                    MacroConfigurationSerializer.CurrentFileExtension),
                cancellationToken);
        }

        private string GetAutosavePath(
            PlayerMacroConfiguration configuration,
            string extension) =>
            Path.Combine(
                autosaveDirectory,
                $"{configuration.Client.Name}-Autosave{extension}");

        private void DeleteBrokenAutosave(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                logger.LogException(exception);
                logger.LogWarn(
                    $"Unable to delete autosave file: {filePath}");
            }
        }
    }
}
