using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Runtime;
using SleepHunter.Settings;

namespace SleepHunter.ViewModels
{
    public sealed partial class ClientListItemViewModel :
        ObservableObject,
        IDisposable
    {
        private readonly IPlayerMacroConfigurationMapper
            configurationMapper;
        private readonly Func<UserSettings> getSettings;
        private readonly IRuntimeAutomationSetupFactory setupFactory;
        private bool isDisposed;

        public ClientListItemViewModel(
            Player player,
            ClientRuntimeViewModel runtime = null)
            : this(
                player,
                macroConfiguration: null,
                runtime,
                configurationMapper: null,
                setupFactory: null,
                getSettings: null)
        {
        }

        internal ClientListItemViewModel(
            Player player,
            PlayerMacroConfiguration macroConfiguration,
            ClientRuntimeViewModel runtime,
            IPlayerMacroConfigurationMapper configurationMapper,
            IRuntimeAutomationSetupFactory setupFactory,
            Func<UserSettings> getSettings)
        {
            Player = player ??
                throw new ArgumentNullException(nameof(player));
            MacroConfiguration = macroConfiguration;
            MacroEditor = macroConfiguration is null
                ? null
                : new MacroEditorViewModel(
                    macroConfiguration,
                    () => IsMacroEditingEnabled);
            this.configurationMapper = configurationMapper;
            this.setupFactory = setupFactory;
            this.getSettings = getSettings;

            Player.PropertyChanged += OnObservedPropertyChanged;
            Player.Location.PropertyChanged += OnObservedPropertyChanged;
            Player.Stats.PropertyChanged += OnObservedPropertyChanged;
            if (MacroConfiguration is not null)
            {
                MacroConfiguration.PropertyChanged +=
                    OnObservedPropertyChanged;
            }

            SetRuntime(runtime);
        }

        public Player Player { get; }

        public PlayerMacroConfiguration MacroConfiguration { get; }

        public MacroEditorViewModel MacroEditor { get; }

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
        [NotifyPropertyChangedFor(nameof(RuntimeDetailsText))]
        public partial Exception LastAutomationError { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLastErrorStatus))]
        [NotifyPropertyChangedFor(nameof(RuntimeDetailsText))]
        public partial string LastErrorStatus { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RuntimeDetailsText))]
        public partial SnapshotCaptureError LastCaptureError
        {
            get;
            private set;
        }

        [ObservableProperty]
        public partial bool IsRuntimeDetailsOpen { get; set; }

        [ObservableProperty]
        public partial string RuntimeDetailsSnapshot { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsMacroEditingEnabled))]
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
            Runtime?.Current?.Lifecycle == MacroLifecycle.Running;

        public bool IsMacroPaused =>
            Runtime?.Current?.Lifecycle == MacroLifecycle.Paused;

        public string MacroToggleLabel =>
            IsMacroRunning
                ? "Pause Macro"
                : IsMacroPaused
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

        public bool HasLastErrorStatus =>
            !string.IsNullOrWhiteSpace(LastErrorStatus);

        public bool UsesRuntimeSnapshot =>
            Runtime?.PresentationSnapshot is not null;

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
                if (error?.Failure ==
                    SnapshotCaptureFailure.LocationTransition)
                {
                    return
                        "Runtime is waiting for a coherent map location";
                }

                return error is null
                    ? "Runtime capture is unavailable"
                    : $"Runtime capture failed: {error.Failure} ({error.Message})";
            }
        }

        public string RuntimeDetailsText => BuildRuntimeDetailsText();

        private ClientSnapshot ObservedSnapshot =>
            Runtime?.PresentationSnapshot;

        public void Dispose()
        {
            if (isDisposed)
                return;

            StopMacroCommand.Cancel();
            ToggleMacroCommand.Cancel();
            SetRuntime(null);
            Player.PropertyChanged -= OnObservedPropertyChanged;
            Player.Location.PropertyChanged -= OnObservedPropertyChanged;
            Player.Stats.PropertyChanged -= OnObservedPropertyChanged;
            if (MacroConfiguration is not null)
            {
                MacroConfiguration.PropertyChanged -=
                    OnObservedPropertyChanged;
            }

            MacroEditor?.Dispose();
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
            if (value is null)
                IsRuntimeDetailsOpen = false;

            if (value is not null)
                value.PropertyChanged += OnRuntimePropertyChanged;

            RecordRuntimeError();
            NotifyObservedState();
        }

        partial void OnIsRuntimeDetailsOpenChanged(bool value)
        {
            if (value)
                RuntimeDetailsSnapshot = BuildRuntimeDetailsText();
        }

        partial void OnLastAutomationErrorChanged(Exception value)
        {
            if (value is not null)
                LastErrorStatus = $"Automation: {value.Message}";
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
                RecordRuntimeError();
                UpdateMacroSpellObservations();
                NotifyObservedState();
            }
        }

        private void RecordRuntimeError()
        {
            var error = Runtime?.CaptureError;
            if (error is null ||
                error.Failure ==
                    SnapshotCaptureFailure.LocationTransition)
            {
                return;
            }

            LastCaptureError = error;
            LastErrorStatus =
                $"Capture {error.Failure}: {error.Message}";
        }

        private string BuildRuntimeDetailsText()
        {
            var details = new StringBuilder();
            details.AppendLine("SleepHunter Runtime Diagnostics");
            details.AppendLine($"Character: {Name ?? "Unknown"}");
            details.AppendLine($"Process ID: {Process.ProcessId}");
            details.AppendLine($"Window: {Process.WindowTitle ?? "Unknown"}");
            details.AppendLine($"Status: {RuntimeStatus}");

            if (Runtime is null)
            {
                details.AppendLine("Runtime attached: No");
                return details.ToString().TrimEnd();
            }

            details.AppendLine("Runtime attached: Yes");
            details.AppendLine($"Client: {Runtime.Client}");
            if (Runtime.Current is { } current)
            {
                details.AppendLine($"Macro lifecycle: {current.Lifecycle}");
                details.AppendLine($"Macro revision: {current.Revision}");
                details.AppendLine($"Macro stop reason: {current.StopReason}");
            }
            else
            {
                details.AppendLine("Macro lifecycle: Waiting");
            }

            if (Runtime.LatestCaptureResult is { } capture)
            {
                var metrics = capture.Metrics;
                var reads = metrics.Reads;
                details.AppendLine($"Capture sequence: {metrics.Sequence.Value}");
                details.AppendLine($"Capture quality: {capture.Quality}");
                details.AppendLine(
                    $"Capture duration: {metrics.Duration.TotalMilliseconds:0.###} ms");
                details.AppendLine(
                    $"Memory reads: {reads.RequestCount} requests, " +
                    $"{reads.TransportReadCount} transport reads, " +
                    $"{reads.FailedReadCount} failed");
                details.AppendLine(
                    $"Memory bytes: {reads.BytesRead} read of " +
                    $"{reads.RequestedBytes} requested");
                AppendCaptureError(
                    details,
                    "Current capture error",
                    capture.Error);
            }
            else
            {
                details.AppendLine("Capture: Waiting");
            }

            if (LastCaptureError is not null &&
                !Equals(
                    Runtime.LatestCaptureResult?.Error,
                    LastCaptureError))
            {
                AppendCaptureError(
                    details,
                    "Last retained capture error",
                    LastCaptureError);
            }

            if (LastAutomationError is not null)
            {
                details.AppendLine();
                details.AppendLine("Last automation error");
                details.AppendLine(
                    $"Exception: {LastAutomationError.GetType().FullName}");
                details.AppendLine(
                    $"Message: {LastAutomationError.Message}");
            }

            return details.ToString().TrimEnd();
        }

        private static void AppendCaptureError(
            StringBuilder details,
            string heading,
            SnapshotCaptureError error)
        {
            if (error is null)
                return;

            details.AppendLine();
            details.AppendLine(heading);
            details.AppendLine($"Section: {error.Section}");
            details.AppendLine($"Failure: {error.Failure}");
            details.AppendLine($"Message: {error.Message}");
            if (error.VariableKey is not null)
                details.AppendLine($"Variable: {error.VariableKey}");

            var mappedError = error.ReadError;
            if (mappedError is null)
                return;

            details.AppendLine(
                $"Mapped read failure: {mappedError.Failure}");
            details.AppendLine(
                $"Mapped variable: {mappedError.VariableKey}");
            if (mappedError.ExpectedKind is { } expectedKind)
                details.AppendLine($"Expected kind: {expectedKind}");
            if (mappedError.ActualKind is { } actualKind)
                details.AppendLine($"Actual kind: {actualKind}");

            var memoryError = mappedError.MemoryError;
            if (memoryError is null)
                return;

            details.AppendLine($"Memory failure: {memoryError.Failure}");
            details.AppendLine($"Address: {memoryError.Address}");
            details.AppendLine(
                $"Requested bytes: {memoryError.RequestedBytes}");
            details.AppendLine($"Bytes read: {memoryError.BytesRead}");
            details.AppendLine(
                $"Native error code: {memoryError.NativeErrorCode}");
        }

        private void UpdateMacroSpellObservations()
        {
            var spellbook = Runtime?.LatestSnapshot?.Spellbook;
            if (spellbook is null || MacroConfiguration is null)
                return;

            var readiness =
                Runtime.Current?.SpellCast?.Plan.Readiness;
            foreach (var queuedSpell in MacroConfiguration.QueuedSpells)
            {
                var observed = spellbook.Find(queuedSpell.Name);
                if (observed is null)
                    continue;

                var spellReadiness = readiness?.FirstOrDefault(
                    entry => string.Equals(
                        entry.Entry.Name,
                        queuedSpell.Name,
                        StringComparison.OrdinalIgnoreCase));
                queuedSpell.MaximumLevel = observed.MaximumLevel;
                queuedSpell.CurrentLevel = observed.CurrentLevel;
                queuedSpell.IsOnCooldown =
                    observed.IsActionDelayed ||
                    spellReadiness?.Status ==
                        SpellReadinessStatus.CoolingDown;
                queuedSpell.IsWaitingOnHealth =
                    spellReadiness?.Status ==
                        SpellReadinessStatus.WaitingForHealth;
            }
        }

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
            MacroConfiguration is not null &&
            configurationMapper is not null &&
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

            var macroConfiguration = MacroConfiguration ??
                throw new InvalidOperationException(
                    "The editable macro configuration is unavailable.");
            var settings = getSettings?.Invoke() ??
                throw new InvalidOperationException(
                    "The current user settings are unavailable.");
            var configuration = configurationMapper?.CreateSnapshot(
                macroConfiguration) ??
                throw new InvalidOperationException(
                    "The macro configuration mapper is unavailable.");
            var setup = setupFactory?.Create(
                configuration,
                settings,
                snapshot.Character.Class) ??
                throw new InvalidOperationException(
                    "The runtime automation setup is unavailable.");

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
            OnPropertyChanged(nameof(RuntimeDetailsText));
            OnPropertyChanged(nameof(MacroToggleLabel));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(UsesRuntimeSnapshot));
            StopMacroCommand.NotifyCanExecuteChanged();
            ToggleMacroCommand.NotifyCanExecuteChanged();
            MacroEditor?.NotifyEditingStateChanged();
        }
    }
}
