using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Macro;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Logging;

namespace SleepHunter.ViewModels
{
    public sealed partial class MacroPersistenceViewModel :
        ObservableObject,
        IDisposable
    {
        private readonly Func<ClientListItemViewModel>
            getSelectedClient;
        private readonly IMacroConfigurationInteraction interaction;
        private readonly ILogger logger;
        private readonly IMacroConfigurationPersistenceService persistence;
        private bool isDisposed;

        public MacroPersistenceViewModel(
            Func<ClientListItemViewModel> getSelectedClient,
            IMacroConfigurationPersistenceService persistence,
            IMacroConfigurationInteraction interaction,
            ILogger logger)
        {
            this.getSelectedClient = getSelectedClient ??
                throw new ArgumentNullException(
                    nameof(getSelectedClient));
            this.persistence = persistence ??
                throw new ArgumentNullException(
                    nameof(persistence));
            this.interaction = interaction ??
                throw new ArgumentNullException(
                    nameof(interaction));
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadMacroCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveMacroCommand))]
        public partial bool IsRunning
        {
            get;
            private set;
        }

        [ObservableProperty]
        public partial Exception LastError
        {
            get;
            private set;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MacroContentColumnSpan))]
        [NotifyCanExecuteChangedFor(nameof(ShowSpellQueueCommand))]
        [NotifyCanExecuteChangedFor(nameof(HideSpellQueueCommand))]
        public partial bool IsSpellQueueVisible
        {
            get;
            set;
        }

        public int MacroContentColumnSpan =>
            IsSpellQueueVisible
                ? 1
                : 2;

        public void Dispose()
        {
            if (isDisposed)
                return;

            LoadMacroCommand.Cancel();
            SaveMacroCommand.Cancel();
            isDisposed = true;
            NotifyStateChanged();
        }

        public void NotifyStateChanged()
        {
            LoadMacroCommand.NotifyCanExecuteChanged();
            SaveMacroCommand.NotifyCanExecuteChanged();
            ShowSpellQueueCommand.NotifyCanExecuteChanged();
            HideSpellQueueCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanLoadMacro))]
        private async Task LoadMacroAsync(
            CancellationToken cancellationToken)
        {
            var configuration =
                getSelectedClient()?.MacroConfiguration;
            if (configuration is null)
                return;

            await ExecuteAsync(
                async () =>
                {
                    logger.LogInfo(
                        $"User has requested to load the macro configuration for character: {configuration.Client.Name}");
                    var filePath = interaction.SelectLoadFile(
                        configuration.Client.Name);
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        logger.LogInfo(
                            "User has cancelled the load macro dialog");
                        return;
                    }

                    var applied = await persistence.LoadAsync(
                        configuration,
                        filePath,
                        cancellationToken);
                    UpdateSpellQueueVisibility(configuration);
                    if (applied.HotkeyRegistrationFailed)
                    {
                        interaction.ShowMessage(
                            "Macro Hotkey Unavailable",
                            "The macro configuration was loaded, but its hotkey could not be registered.",
                            "Choose another hotkey before starting the macro.");
                    }
                },
                "Failed to Load Macro",
                $"Unable to load the macro configuration for {configuration.Client.Name}.",
                cancellationToken);
        }

        private bool CanLoadMacro()
        {
            var selectedClient = getSelectedClient();
            return !isDisposed &&
                   !IsRunning &&
                   selectedClient?.MacroConfiguration is not null &&
                   selectedClient.IsMacroEditingEnabled;
        }

        [RelayCommand(CanExecute = nameof(CanSaveMacro))]
        private async Task SaveMacroAsync(
            CancellationToken cancellationToken)
        {
            var configuration =
                getSelectedClient()?.MacroConfiguration;
            if (configuration is null)
                return;

            await ExecuteAsync(
                async () =>
                {
                    logger.LogInfo(
                        $"User has requested to save the macro configuration for character: {configuration.Client.Name}");
                    var filePath = interaction.SelectSaveFile(
                        configuration.Client.Name);
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        logger.LogInfo(
                            "User has cancelled the save macro dialog");
                        return;
                    }

                    await persistence.SaveAsync(
                        configuration,
                        filePath,
                        cancellationToken);
                },
                "Failed to Save Macro",
                $"Unable to save the macro configuration for {configuration.Client.Name}.",
                cancellationToken);
        }

        private bool CanSaveMacro()
        {
            var selectedClient = getSelectedClient();
            return !isDisposed &&
                   !IsRunning &&
                   selectedClient?.MacroConfiguration is not null &&
                   selectedClient.IsLoggedIn;
        }

        [RelayCommand(CanExecute = nameof(CanShowSpellQueue))]
        private void ShowSpellQueue() =>
            IsSpellQueueVisible = true;

        private bool CanShowSpellQueue() =>
            !isDisposed &&
            !IsSpellQueueVisible;

        [RelayCommand(CanExecute = nameof(CanHideSpellQueue))]
        private void HideSpellQueue() =>
            IsSpellQueueVisible = false;

        private bool CanHideSpellQueue() =>
            !isDisposed &&
            IsSpellQueueVisible;

        public async Task AutoLoadMacroAsync(
            PlayerMacroConfiguration configuration,
            bool showError = true,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ObjectDisposedException.ThrowIf(isDisposed, this);

            LastError = null;
            try
            {
                var loaded = await persistence.AutoLoadAsync(
                    configuration,
                    cancellationToken);
                if (loaded is null)
                    return;

                UpdateSpellQueueVisibility(configuration);
                if (loaded.Applied.HotkeyRegistrationFailed &&
                    showError)
                {
                    interaction.ShowMessage(
                        "Macro Hotkey Unavailable",
                        "The macro configuration was loaded, but its hotkey could not be registered.",
                        "Choose another hotkey before starting the macro.");
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LastError = exception;
                logger.LogException(exception);
                logger.LogError(
                    $"Unable to auto-load the macro configuration for {configuration.Client.Name}");
                if (showError)
                {
                    interaction.ShowMessage(
                        "Failed to Load Macro",
                        $"Unable to load the macro configuration for {configuration.Client.Name}.",
                        exception.Message);
                }
            }
        }

        public async Task AutoSaveMacroAsync(
            PlayerMacroConfiguration configuration,
            bool showError = true,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ObjectDisposedException.ThrowIf(isDisposed, this);

            LastError = null;
            try
            {
                await persistence.AutoSaveAsync(
                    configuration,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LastError = exception;
                logger.LogException(exception);
                logger.LogError(
                    $"Unable to auto-save the macro configuration for {configuration.Client.Name}");
                if (showError)
                {
                    interaction.ShowMessage(
                        "Failed to Autosave",
                        $"Unable to save macro configuration for {configuration.Client.Name}.",
                        exception.Message);
                }
            }
        }

        private async Task ExecuteAsync(
            Func<Task> action,
            string errorTitle,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            IsRunning = true;
            LastError = null;

            try
            {
                await action();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                LastError = exception;
                logger.LogException(exception);
                logger.LogError(errorMessage);
                interaction.ShowMessage(
                    errorTitle,
                    errorMessage,
                    exception.Message);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void UpdateSpellQueueVisibility(
            PlayerMacroConfiguration configuration)
        {
            if (ReferenceEquals(
                    getSelectedClient()?.MacroConfiguration,
                    configuration))
            {
                IsSpellQueueVisible =
                    configuration.QueuedSpells.Count > 0;
            }
        }
    }
}
