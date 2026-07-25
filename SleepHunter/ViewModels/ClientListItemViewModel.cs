using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Services.Runtime;
using SleepHunter.Settings;

namespace SleepHunter.ViewModels
{
    public sealed partial class ClientListItemViewModel :
        ObservableObject,
        IDisposable
    {
        private readonly IRuntimeMacroConfigurationAdapter
            configurationAdapter;
        private readonly Func<UserSettings> getSettings;
        private readonly IRuntimeAutomationSetupFactory setupFactory;
        private bool isDisposed;

        public ClientListItemViewModel(
            Player player,
            ClientRuntimeViewModel runtime = null)
            : this(
                player,
                macroState: null,
                runtime,
                configurationAdapter: null,
                setupFactory: null,
                getSettings: null)
        {
        }

        internal ClientListItemViewModel(
            Player player,
            PlayerMacroState macroState,
            ClientRuntimeViewModel runtime,
            IRuntimeMacroConfigurationAdapter configurationAdapter,
            IRuntimeAutomationSetupFactory setupFactory,
            Func<UserSettings> getSettings)
        {
            Player = player ??
                throw new ArgumentNullException(nameof(player));
            MacroState = macroState;
            this.configurationAdapter = configurationAdapter;
            this.setupFactory = setupFactory;
            this.getSettings = getSettings;

            Player.PropertyChanged += OnObservedPropertyChanged;
            Player.Location.PropertyChanged += OnObservedPropertyChanged;
            Player.Stats.PropertyChanged += OnObservedPropertyChanged;
            if (MacroState is not null)
                MacroState.PropertyChanged += OnObservedPropertyChanged;

            SetRuntime(runtime);
        }

        public Player Player { get; }

        public PlayerMacroState MacroState { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasRuntime))]
        [NotifyPropertyChangedFor(nameof(RuntimeStatus))]
        [NotifyPropertyChangedFor(nameof(UsesRuntimeSnapshot))]
        public partial ClientRuntimeViewModel Runtime
        {
            get;
            private set;
        }

        [ObservableProperty]
        public partial Exception LastAutomationError { get; private set; }

        [ObservableProperty]
        public partial MacroConfigurationLoadResult LastConfigurationLoad
        {
            get;
            private set;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsMacroEditingEnabled))]
        [NotifyCanExecuteChangedFor(nameof(StartOrResumeMacroCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseMacroCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopMacroCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleMacroCommand))]
        public partial bool IsAutomationCommandRunning
        {
            get;
            private set;
        }

        public ClientProcess Process => Player.Process;

        public ClientState GameClient => Player.GameClient;

        public Inventory Inventory => Player.Inventory;

        public EquipmentSet Equipment => Player.Equipment;

        public Skillbook Skillbook => Player.Skillbook;

        public Spellbook Spellbook => Player.Spellbook;

        public string Name =>
            ObservedSnapshot?.Character?.Name ??
            Player.Name;

        public string Status => Player.Status;

        public bool IsLoggedIn =>
            ObservedSnapshot is { } snapshot
                ? snapshot.Presence == ClientPresence.InWorld
                : Player.IsLoggedIn;

        public bool IsMacroRunning =>
            Runtime is null
                ? Player.IsMacroRunning
                : Runtime.Current?.Lifecycle == MacroLifecycle.Running;

        public bool IsMacroPaused =>
            Runtime is null
                ? Player.IsMacroPaused
                : Runtime.Current?.Lifecycle == MacroLifecycle.Paused;

        public string StartMacroLabel =>
            IsMacroPaused
                ? "Resume Macro"
                : "Start Macro";

        public bool IsMacroEditingEnabled =>
            IsLoggedIn &&
            !IsMacroRunning &&
            !IsAutomationCommandRunning;

        public bool HasHotkey => Player.HasHotkey;

        public string HotkeyString => Player.HotkeyString;

        public string MapName =>
            ObservedSnapshot?.Location?.MapName ??
            Player.Location.MapName;

        public int MapX =>
            ObservedSnapshot?.Location?.X ??
            Player.Location.X;

        public int MapY =>
            ObservedSnapshot?.Location?.Y ??
            Player.Location.Y;

        public int CurrentHealth =>
            ObservedSnapshot?.Vitals?.CurrentHealth ??
            Player.Stats.CurrentHealth;

        public int MaximumHealth =>
            ObservedSnapshot?.Vitals?.MaximumHealth ??
            Player.Stats.MaximumHealth;

        public double HealthPercent =>
            ObservedSnapshot?.Vitals?.HealthPercent ??
            Player.Stats.HealthPercent;

        public int CurrentMana =>
            ObservedSnapshot?.Vitals?.CurrentMana ??
            Player.Stats.CurrentMana;

        public int MaximumMana =>
            ObservedSnapshot?.Vitals?.MaximumMana ??
            Player.Stats.MaximumMana;

        public double ManaPercent =>
            ObservedSnapshot?.Vitals?.ManaPercent ??
            Player.Stats.ManaPercent;

        public bool HasRuntime => Runtime is not null;

        public bool UsesRuntimeSnapshot =>
            Runtime?.IsCaptureHealthy == true &&
            Runtime.LatestSnapshot is not null;

        public string RuntimeStatus
        {
            get
            {
                if (Runtime is null)
                    return "Legacy client observation";

                if (!Runtime.HasCapture)
                    return "Runtime is waiting for its first snapshot";

                var result = Runtime.LatestCaptureResult;
                if (result?.Succeeded == true)
                {
                    return
                        $"Runtime snapshot {result.Metrics.Sequence.Value}, {result.Metrics.Duration.TotalMilliseconds:0.###} ms";
                }

                var error = result?.Error;
                return error is null
                    ? "Runtime capture is unavailable"
                    : $"Runtime capture failed: {error.Failure} ({error.Message})";
            }
        }

        private ClientSnapshot ObservedSnapshot =>
            UsesRuntimeSnapshot
                ? Runtime.LatestSnapshot
                : null;

        public void Dispose()
        {
            if (isDisposed)
                return;

            StartOrResumeMacroCommand.Cancel();
            PauseMacroCommand.Cancel();
            StopMacroCommand.Cancel();
            ToggleMacroCommand.Cancel();
            SetRuntime(null);
            Player.PropertyChanged -= OnObservedPropertyChanged;
            Player.Location.PropertyChanged -= OnObservedPropertyChanged;
            Player.Stats.PropertyChanged -= OnObservedPropertyChanged;
            if (MacroState is not null)
                MacroState.PropertyChanged -= OnObservedPropertyChanged;

            isDisposed = true;
        }

        internal void SetRuntime(ClientRuntimeViewModel value)
        {
            Runtime = value;
        }

        partial void OnRuntimeChanging(ClientRuntimeViewModel value)
        {
            if (Runtime is not null)
                Runtime.PropertyChanged -= OnRuntimePropertyChanged;
        }

        partial void OnRuntimeChanged(ClientRuntimeViewModel value)
        {
            if (value is not null)
                value.PropertyChanged += OnRuntimePropertyChanged;

            NotifyObservedState();
        }

        private void OnObservedPropertyChanged(
            object sender,
            PropertyChangedEventArgs e) =>
            NotifyObservedState();

        private void OnRuntimePropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (string.Equals(
                    e.PropertyName,
                    nameof(ClientRuntimeViewModel.LatestCapture),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientRuntimeViewModel.Current),
                    StringComparison.Ordinal))
            {
                NotifyObservedState();
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartOrResumeMacro))]
        private Task StartOrResumeMacroAsync(
            CancellationToken cancellationToken) =>
            ExecuteAutomationAsync(
                StartOrResumeMacroCoreAsync,
                cancellationToken);

        [RelayCommand(CanExecute = nameof(CanPauseMacro))]
        private Task PauseMacroAsync(
            CancellationToken cancellationToken) =>
            ExecuteAutomationAsync(
                PauseMacroCoreAsync,
                cancellationToken);

        [RelayCommand(CanExecute = nameof(CanStopMacro))]
        private Task StopMacroAsync(
            CancellationToken cancellationToken) =>
            ExecuteAutomationAsync(
                StopMacroCoreAsync,
                cancellationToken);

        [RelayCommand(CanExecute = nameof(CanToggleMacro))]
        private Task ToggleMacroAsync(
            CancellationToken cancellationToken) =>
            ExecuteAutomationAsync(
                IsMacroRunning
                    ? PauseMacroCoreAsync
                    : StartOrResumeMacroCoreAsync,
                cancellationToken);

        private bool CanStartOrResumeMacro() =>
            !IsAutomationCommandRunning &&
            CanPrepareAutomation() &&
            (Runtime.StartCommand.CanExecute(null) ||
             Runtime.ResumeCommand.CanExecute(null));

        private bool CanPauseMacro() =>
            !IsAutomationCommandRunning &&
            Runtime?.PauseCommand.CanExecute(null) == true;

        private bool CanStopMacro() =>
            !IsAutomationCommandRunning &&
            Runtime?.StopCommand.CanExecute(null) == true;

        private bool CanToggleMacro() =>
            CanStartOrResumeMacro() ||
            CanPauseMacro();

        private bool CanPrepareAutomation() =>
            MacroState is not null &&
            configurationAdapter is not null &&
            setupFactory is not null &&
            getSettings is not null &&
            Runtime?.IsCaptureHealthy == true &&
            Runtime.LatestSnapshot is
            {
                Presence: ClientPresence.InWorld,
                Character: not null
            };

        private async Task StartOrResumeMacroCoreAsync(
            CancellationToken cancellationToken)
        {
            var runtime = Runtime ??
                throw new InvalidOperationException(
                    "The client runtime is unavailable.");
            var snapshot = runtime.LatestSnapshot;
            if (runtime.IsCaptureHealthy != true ||
                snapshot is not
                {
                    Presence: ClientPresence.InWorld,
                    Character: not null
                })
            {
                throw new InvalidOperationException(
                    "A healthy in-world client snapshot is required to start automation.");
            }

            var macroState = MacroState ??
                throw new InvalidOperationException(
                    "The editable macro state is unavailable.");
            var settings = getSettings?.Invoke() ??
                throw new InvalidOperationException(
                    "The current user settings are unavailable.");
            var loaded = configurationAdapter?.Adapt(macroState) ??
                throw new InvalidOperationException(
                    "The macro configuration adapter is unavailable.");
            var setup = setupFactory?.Create(
                loaded.Configuration,
                settings,
                snapshot.Character.Class) ??
                throw new InvalidOperationException(
                    "The runtime automation setup is unavailable.");

            LastConfigurationLoad = loaded;
            await runtime
                .SendCommandAsync(
                    setup.ReplaceQueues,
                    cancellationToken)
                .ConfigureAwait(false);
            await runtime
                .SendCommandAsync(
                    setup.ConfigureAutomation,
                    cancellationToken)
                .ConfigureAwait(false);

            MacroCommand lifecycleCommand =
                runtime.Current?.Lifecycle switch
                {
                    MacroLifecycle.Stopped =>
                        new StartMacroCommand(),
                    MacroLifecycle.Paused =>
                        new ResumeMacroCommand(),
                    _ => throw new InvalidOperationException(
                        "Automation can only start from a stopped or paused runtime.")
                };
            await runtime
                .SendCommandAsync(
                    lifecycleCommand,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private Task PauseMacroCoreAsync(
            CancellationToken cancellationToken) =>
            SendLifecycleCommandAsync(
                new PauseMacroCommand(),
                Runtime?.PauseCommand.CanExecute(null) == true,
                cancellationToken);

        private Task StopMacroCoreAsync(
            CancellationToken cancellationToken) =>
            SendLifecycleCommandAsync(
                new StopMacroCommand(),
                Runtime?.StopCommand.CanExecute(null) == true,
                cancellationToken);

        private Task SendLifecycleCommandAsync(
            MacroCommand command,
            bool canExecute,
            CancellationToken cancellationToken)
        {
            if (!canExecute || Runtime is null)
            {
                throw new InvalidOperationException(
                    "The requested runtime lifecycle change is unavailable.");
            }

            return Runtime
                .SendCommandAsync(command, cancellationToken)
                .AsTask();
        }

        private async Task ExecuteAutomationAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            IsAutomationCommandRunning = true;
            LastAutomationError = null;

            try
            {
                await action(cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                LastAutomationError = exception;
            }
            finally
            {
                IsAutomationCommandRunning = false;
            }
        }

        private void NotifyObservedState()
        {
            OnPropertyChanged(nameof(CurrentHealth));
            OnPropertyChanged(nameof(CurrentMana));
            OnPropertyChanged(nameof(GameClient));
            OnPropertyChanged(nameof(HasHotkey));
            OnPropertyChanged(nameof(HasRuntime));
            OnPropertyChanged(nameof(HealthPercent));
            OnPropertyChanged(nameof(HotkeyString));
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(IsMacroEditingEnabled));
            OnPropertyChanged(nameof(IsMacroPaused));
            OnPropertyChanged(nameof(IsMacroRunning));
            OnPropertyChanged(nameof(ManaPercent));
            OnPropertyChanged(nameof(MapName));
            OnPropertyChanged(nameof(MapX));
            OnPropertyChanged(nameof(MapY));
            OnPropertyChanged(nameof(MaximumHealth));
            OnPropertyChanged(nameof(MaximumMana));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(RuntimeStatus));
            OnPropertyChanged(nameof(StartMacroLabel));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(UsesRuntimeSnapshot));
            StartOrResumeMacroCommand.NotifyCanExecuteChanged();
            PauseMacroCommand.NotifyCanExecuteChanged();
            StopMacroCommand.NotifyCanExecuteChanged();
            ToggleMacroCommand.NotifyCanExecuteChanged();
        }
    }
}
