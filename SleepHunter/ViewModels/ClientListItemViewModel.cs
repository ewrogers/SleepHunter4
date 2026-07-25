using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Models;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels
{
    public sealed class ClientListItemViewModel :
        ObservableObject,
        IDisposable
    {
        private ClientRuntimeViewModel runtime;
        private bool isDisposed;

        public ClientListItemViewModel(
            Player player,
            ClientRuntimeViewModel runtime = null)
        {
            Player = player ??
                throw new ArgumentNullException(nameof(player));

            Player.PropertyChanged += OnObservedPropertyChanged;
            Player.Location.PropertyChanged += OnObservedPropertyChanged;
            Player.Stats.PropertyChanged += OnObservedPropertyChanged;
            SetRuntime(runtime);
        }

        public Player Player { get; }

        public ClientRuntimeViewModel Runtime => runtime;

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

        public bool IsMacroRunning => Player.IsMacroRunning;

        public bool IsMacroPaused => Player.IsMacroPaused;

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

            SetRuntime(null);
            Player.PropertyChanged -= OnObservedPropertyChanged;
            Player.Location.PropertyChanged -= OnObservedPropertyChanged;
            Player.Stats.PropertyChanged -= OnObservedPropertyChanged;
            isDisposed = true;
        }

        internal void SetRuntime(ClientRuntimeViewModel value)
        {
            if (ReferenceEquals(runtime, value))
                return;

            if (runtime is not null)
                runtime.PropertyChanged -= OnRuntimePropertyChanged;

            runtime = value;

            if (runtime is not null)
                runtime.PropertyChanged += OnRuntimePropertyChanged;

            NotifyObservedState();
            OnPropertyChanged(nameof(Runtime));
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
                    StringComparison.Ordinal))
            {
                NotifyObservedState();
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
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(UsesRuntimeSnapshot));
        }
    }
}
