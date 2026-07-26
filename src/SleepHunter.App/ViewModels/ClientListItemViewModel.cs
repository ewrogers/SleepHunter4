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
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Characters;
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
        private readonly IUiDispatcher uiDispatcher;
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
                getSettings: null,
                uiDispatcher: null)
        {
        }

        internal ClientListItemViewModel(
            Player player,
            PlayerMacroConfiguration macroConfiguration,
            ClientRuntimeViewModel runtime,
            IPlayerMacroConfigurationMapper configurationMapper,
            IRuntimeAutomationSetupFactory setupFactory,
            Func<UserSettings> getSettings,
            IUiDispatcher uiDispatcher = null)
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
            this.uiDispatcher = uiDispatcher;

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
        [NotifyPropertyChangedFor(nameof(IsRuntimeStatusError))]
        [NotifyPropertyChangedFor(nameof(RuntimeStatus))]
        [NotifyPropertyChangedFor(nameof(UsesRuntimeSnapshot))]
        [NotifyPropertyChangedFor(nameof(CanReplaceMacroConfiguration))]
        public partial ClientRuntimeViewModel Runtime
        {
            get;
            private set;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRuntimeStatusError))]
        [NotifyPropertyChangedFor(nameof(RuntimeStatus))]
        [NotifyPropertyChangedFor(nameof(RuntimeDetailsText))]
        public partial Exception LastAutomationError { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRuntimeStatusError))]
        [NotifyPropertyChangedFor(nameof(RuntimeStatus))]
        [NotifyPropertyChangedFor(nameof(RuntimeDetailsText))]
        public partial Exception LastObservationError { get; private set; }

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
        [NotifyPropertyChangedFor(nameof(CanReplaceMacroConfiguration))]
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
            !IsAutomationCommandRunning;

        public bool CanReplaceMacroConfiguration =>
            IsMacroEditingEnabled &&
            !IsMacroRunning;

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

        public bool IsRuntimeStatusError
        {
            get
            {
                if (LastAutomationError is not null)
                    return true;

                if (LastObservationError is not null)
                    return true;

                if (Runtime is
                {
                    RuntimeFailure: not null
                } or
                {
                    IsHostAvailable: false
                })
                {
                    return true;
                }

                var result = Runtime?.LatestCaptureResult;
                return result is { Succeeded: false } &&
                       result.Error?.Failure !=
                           SnapshotCaptureFailure.LocationTransition;
            }
        }

        public string RuntimeStatus
        {
            get
            {
                if (LastAutomationError is { } automationError)
                    return $"Automation error: {automationError.Message}";

                if (LastObservationError is { } observationError)
                    return $"Observation error: {observationError.Message}";

                if (Runtime is null)
                    return "Unavailable";

                if (Runtime.RuntimeFailure is { } runtimeFailure)
                    return $"Runtime stopped: {runtimeFailure.Message}";

                if (!Runtime.IsHostAvailable)
                    return "Runtime stopped unexpectedly";

                if (!Runtime.HasCapture)
                    return "Waiting";

                var result = Runtime.LatestCaptureResult;
                if (result?.Succeeded == true)
                    return "Healthy";

                var error = result?.Error;
                if (error?.Failure ==
                    SnapshotCaptureFailure.LocationTransition)
                {
                    return "Waiting for coherent map location";
                }

                return error is null
                    ? "Unavailable"
                    : $"{error.Failure}: {error.Message}";
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
            PropertyChangedEventArgs e)
        {
            _ = ProcessObservedPropertyChangedAsync(sender, e);
        }

        private async Task ProcessObservedPropertyChangedAsync(
            object sender,
            PropertyChangedEventArgs e)
        {
            try
            {
                await InvokeOnUiAsync(
                    () =>
                    {
                        if (!isDisposed)
                            NotifyObservedState();
                    });
                if (isDisposed ||
                    !ReferenceEquals(sender, MacroConfiguration) ||
                    !IsMacroRunning ||
                    !IsRuntimeConfigurationProperty(e.PropertyName))
                {
                    return;
                }

                LastAutomationError = null;
                await ApplyLiveAutomationSetupAsync();
            }
            catch (Exception exception)
            {
                if (isDisposed)
                    return;

                await InvokeOnUiAsync(
                    () => LastAutomationError = exception);
            }
        }

        private ValueTask InvokeOnUiAsync(Action action)
        {
            if (uiDispatcher is not null)
                return uiDispatcher.InvokeAsync(action);

            action();
            return ValueTask.CompletedTask;
        }

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
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientRuntimeViewModel.RuntimeFailure),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientRuntimeViewModel.IsHostAvailable),
                    StringComparison.Ordinal))
            {
                RecordRuntimeError();
                try
                {
                    UpdateMacroFlowerObservations();
                    UpdateMacroSpellObservations();
                    LastObservationError = null;
                }
                catch (Exception exception)
                {
                    LastObservationError = exception;
                    LastErrorStatus =
                        $"Observation: {exception.Message}";
                }
                finally
                {
                    NotifyObservedState();
                }
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
            details.AppendLine(
                $"Runtime available: " +
                $"{(Runtime.IsHostAvailable ? "Yes" : "No")}");
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
                if (capture.Snapshot?.Vitals is { } vitals)
                {
                    details.AppendLine(
                        $"Vitals: HP {vitals.CurrentHealth}/" +
                        $"{vitals.MaximumHealth}, MP " +
                        $"{vitals.CurrentMana}/{vitals.MaximumMana}");
                }

                var duration = Runtime.CaptureStatistics.Duration;
                if (duration.SampleCount > 0)
                {
                    details.AppendLine(
                        $"Timing window: {duration.SampleCount} of " +
                        $"{Runtime.CaptureStatistics.WindowCapacity} captures");
                    details.AppendLine(
                        $"Timing average: {duration.Average.TotalMilliseconds:0.###} ms");
                    details.AppendLine(
                        $"Timing minimum: {duration.Minimum.TotalMilliseconds:0.###} ms");
                    details.AppendLine(
                        $"Timing median: {duration.Median.TotalMilliseconds:0.###} ms");
                    details.AppendLine(
                        $"Timing p95: {duration.Percentile95.TotalMilliseconds:0.###} ms");
                    details.AppendLine(
                        $"Timing maximum: {duration.Maximum.TotalMilliseconds:0.###} ms");
                }

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

            if (LastObservationError is not null)
            {
                details.AppendLine();
                details.AppendLine("Last observation error");
                details.AppendLine(
                    $"Exception: {LastObservationError.GetType().FullName}");
                details.AppendLine(
                    $"Message: {LastObservationError.Message}");
            }

            if (Runtime.RuntimeFailure is { } runtimeFailure)
            {
                details.AppendLine();
                details.AppendLine("Runtime failure");
                details.AppendLine(
                    $"Exception: {runtimeFailure.GetType().FullName}");
                details.AppendLine(
                    $"Message: {runtimeFailure.Message}");
                if (!string.IsNullOrWhiteSpace(runtimeFailure.StackTrace))
                {
                    details.AppendLine("Stack trace:");
                    details.AppendLine(runtimeFailure.StackTrace);
                }
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
            if (MacroConfiguration is null)
                return;

            var spellCast = Runtime?.Current?.SpellCast;
            var readiness = spellCast?.Plan.Readiness;
            var activeEntryId = spellCast is
            {
                Origin: SpellCastOrigin.SpellQueue,
                Status:
                    SpellCastStatus.WaitingForStaff or
                    SpellCastStatus.WaitingForPanel or
                    SpellCastStatus.Casting,
                Plan.SelectedEntry: { } activeEntry
            }
                ? activeEntry.Id
                : (SpellQueueEntryId?)null;
            foreach (var queuedSpell in
                     MacroConfiguration.QueuedSpells.ToArray())
            {
                queuedSpell.IsActive =
                    activeEntryId?.Value == queuedSpell.Id;
                if (spellbook is null)
                    continue;

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

        private void UpdateMacroFlowerObservations()
        {
            var runtime = Runtime;
            if (MacroConfiguration is null)
                return;

            if (runtime?.Current?.Lifecycle ==
                MacroLifecycle.Stopped)
            {
                foreach (var flower in
                         MacroConfiguration.FlowerTargets.ToArray())
                {
                    flower.ResetTimer();
                }

                return;
            }

            var schedules = runtime?.Current?.FlowerSchedules;
            var currentTime =
                runtime?.LatestCapture?.Result.Metrics.CaptureCompletedAt;
            if (schedules is null ||
                currentTime is null)
            {
                return;
            }

            foreach (var flower in
                     MacroConfiguration.FlowerTargets.ToArray())
            {
                if (flower.Id <= 0 ||
                    schedules.GetReadyAt(
                        new FlowerQueueEntryId(flower.Id)) is not
                        { } readyAt)
                {
                    continue;
                }

                flower.UpdateRemainingTime(
                    readyAt.Elapsed - currentTime.Value.Elapsed);
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

            var setup = CreateRuntimeAutomationSetup(
                snapshot.Character.Class);

            await runtime
                .SendCommandAsync(
                    setup.ApplyAutomation,
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

        private async Task ApplyLiveAutomationSetupAsync()
        {
            var runtime = Runtime;
            if (runtime?.Current?.Lifecycle !=
                MacroLifecycle.Running)
            {
                return;
            }

            var character = runtime.LastSuccessfulSnapshot?.Character ??
                throw new InvalidOperationException(
                    "A successful character snapshot is required to update running automation.");
            var setup = CreateRuntimeAutomationSetup(character.Class);
            await runtime
                .SendCommandAsync(setup.ApplyAutomation)
                .ConfigureAwait(true);
        }

        private RuntimeAutomationSetup CreateRuntimeAutomationSetup(
            CharacterClass characterClass)
        {
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
            return setupFactory?.Create(
                    configuration,
                    settings,
                    characterClass) ??
                throw new InvalidOperationException(
                    "The runtime automation setup is unavailable.");
        }

        private static bool IsRuntimeConfigurationProperty(
            string propertyName) =>
            propertyName is
                nameof(PlayerMacroConfiguration.QueuedSpells) or
                nameof(PlayerMacroConfiguration.FlowerTargets) or
                nameof(PlayerMacroConfiguration.Skills) or
                nameof(PlayerMacroConfiguration.SpellQueueRotation) or
                nameof(PlayerMacroConfiguration.UseLyliacVineyard) or
                nameof(PlayerMacroConfiguration.FlowerAlternateCharacters) or
                nameof(PlayerMacroConfiguration.PrioritizeAlternateCharacters) or
                nameof(PlayerMacroConfiguration.MaximumFlowerXDistance) or
                nameof(PlayerMacroConfiguration.MaximumFlowerYDistance);

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
            OnPropertyChanged(nameof(CanReplaceMacroConfiguration));
            OnPropertyChanged(nameof(HasHotkey));
            OnPropertyChanged(nameof(HasRuntime));
            OnPropertyChanged(nameof(HealthPercent));
            OnPropertyChanged(nameof(HotkeyString));
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(IsMacroEditingEnabled));
            OnPropertyChanged(nameof(IsMacroPaused));
            OnPropertyChanged(nameof(IsMacroRunning));
            OnPropertyChanged(nameof(IsRuntimeStatusError));
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
