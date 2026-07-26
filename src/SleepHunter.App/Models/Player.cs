using System;
using System.Linq;
using SleepHunter.Common;
using SleepHunter.Macro;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Settings;

namespace SleepHunter.Models
{
    public sealed class Player :
        ObservableObject,
        IDisposable
    {
        private readonly Inventory inventory = new();
        private readonly EquipmentSet equipment = new();
        private readonly Skillbook skillbook = new();
        private readonly Spellbook spellbook = new();
        private readonly PlayerStats stats = new();
        private readonly MapLocation location = new();

        private ClientLayout layout;
        private string name;
        private DateTime? loginTimestamp;
        private bool isLoggedIn;
        private Hotkey hotkey;
        private int selectedTabIndex;
        private bool hasLyliacPlant;
        private bool hasLyliacVineyard;
        private long lastSnapshotSequence;
        private bool isDisposed;

        public ClientProcess Process { get; init; }

        public ClientLayout Layout
        {
            get => layout;
            set => SetProperty(ref layout, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public Inventory Inventory => inventory;

        public EquipmentSet Equipment => equipment;

        public Skillbook Skillbook => skillbook;

        public Spellbook Spellbook => spellbook;

        public PlayerStats Stats => stats;

        public MapLocation Location => location;

        public bool IsLoggedIn
        {
            get => isLoggedIn;
            set => SetProperty(ref isLoggedIn, value);
        }

        public DateTime? LoginTimestamp
        {
            get => loginTimestamp;
            set => SetProperty(ref loginTimestamp, value);
        }

        public string HotkeyString => hotkey?.ToString();

        public Hotkey Hotkey
        {
            get => hotkey;
            set => SetProperty(
                ref hotkey,
                value,
                onChanged: (_) =>
                {
                    RaisePropertyChanged(nameof(HotkeyString));
                    RaisePropertyChanged(nameof(HasHotkey));
                });
        }

        public bool HasHotkey =>
            !string.IsNullOrWhiteSpace(HotkeyString);

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set => SetProperty(ref selectedTabIndex, value);
        }

        public bool HasLyliacPlant
        {
            get => hasLyliacPlant;
            private set => SetProperty(ref hasLyliacPlant, value);
        }

        public bool HasLyliacVineyard
        {
            get => hasLyliacVineyard;
            private set => SetProperty(
                ref hasLyliacVineyard,
                value);
        }

        public long LastSnapshotSequence
        {
            get => lastSnapshotSequence;
            private set => SetProperty(
                ref lastSnapshotSequence,
                value);
        }

        public Player(ClientProcess process)
        {
            Process = process ??
                throw new ArgumentNullException(nameof(process));
        }

        public void ApplySnapshot(ClientSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (!snapshot.IsUsable ||
                snapshot.Sequence.Value <= LastSnapshotSequence)
            {
                return;
            }

            var character = snapshot.Character;
            var isInWorld =
                snapshot.Presence == ClientPresence.InWorld &&
                !string.IsNullOrWhiteSpace(character?.Name);
            if (!isInWorld)
            {
                SetLoggedOutPresentation();
                LastSnapshotSequence = snapshot.Sequence.Value;
                return;
            }

            Name = character.Name;
            stats.Apply(snapshot.Vitals);
            location.Apply(snapshot.Location);
            inventory.Apply(
                snapshot.Inventory,
                character.Gold);
            equipment.Apply(snapshot.Equipment);
            skillbook.Apply(snapshot.Skillbook);
            spellbook.Apply(snapshot.Spellbook);
            UpdateSpecialSpellFlags(snapshot.Spellbook);
            IsLoggedIn = true;
            LastSnapshotSequence = snapshot.Sequence.Value;
        }

        public void RefreshProcess() => Process.Refresh();

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
        }

        public override string ToString() =>
            Name ??
            string.Format(
                "Process {0}",
                Process.ProcessId.ToString());

        private void SetLoggedOutPresentation()
        {
            IsLoggedIn = false;
            stats.Reset();
            location.Reset();
            inventory.Reset();
            equipment.Reset();
            skillbook.Reset();
            spellbook.Reset();
            HasLyliacPlant = false;
            HasLyliacVineyard = false;
        }

        private void UpdateSpecialSpellFlags(
            SpellbookSnapshot snapshot)
        {
            HasLyliacPlant = snapshot?.Spells.Any(
                spell => string.Equals(
                    spell.Name,
                    Spell.LyliacPlantKey,
                    StringComparison.OrdinalIgnoreCase)) == true;
            HasLyliacVineyard = snapshot?.Spells.Any(
                spell => string.Equals(
                    spell.Name,
                    Spell.LyliacVineyardKey,
                    StringComparison.OrdinalIgnoreCase)) == true;
        }
    }
}
