using System;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;
using SleepHunter.Macro;
using SleepHunter.Settings;
using SleepHunter.Win32;

namespace SleepHunter.Models
{
    public sealed class Player : ObservableObject, IDisposable
    {
        private const string CharacterNameKey = @"CharacterName";
        
        private bool isDisposed;
        private ClientVersion version;
        private readonly ProcessMemoryAccessor accessor;
        private string name;
        private string guild;
        private string guildRank;
        private string title;
        private PlayerClass playerClass;
        private Inventory inventory;
        private EquipmentSet equipment;
        private Skillbook skillbook;
        private Spellbook spellbook;
        private PlayerStats stats;
        private PlayerModifiers modifiers;
        private MapLocation location;
        private ClientState gameClient;
        private DateTime? loginTimestamp;
        private bool isLoggedIn;
        private string status;
        private bool isMacroRunning;
        private bool isMacroPaused;
        private bool isMacroStopped;
        private Hotkey hotkey;
        private int selectedTabIndex;
        private double? skillbookScrollPosition;
        private double? spellbookScrollPosition;
        private double? spellQueueScrollPosition;
        private double? flowerScrollPosition;
        private bool hasLyliacPlant;
        private bool hasLyliacVineyard;
        private bool hasFasSpiorad;
        private DateTime lastFlowerTimestamp;

        public event EventHandler PlayerUpdated;
        public event EventHandler LoggedIn;
        public event EventHandler LoggedOut;

        public ClientProcess Process { get; }

        public ClientVersion Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        public nint ProcessHandle => accessor.ProcessHandle;

        public ProcessMemoryAccessor Accessor => accessor;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string Guild
        {
            get => guild;
            set => SetProperty(ref guild, value);
        }

        public string GuildRank
        {
            get => guildRank;
            set => SetProperty(ref guildRank, value);
        }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public PlayerClass Class
        {
            get => playerClass;
            set => SetProperty(ref playerClass, value);
        }

        public Inventory Inventory
        {
            get => inventory;
            private set => SetProperty(ref inventory, value);
        }

        public EquipmentSet Equipment
        {
            get => equipment;
            set => SetProperty(ref equipment, value);
        }

        public Skillbook Skillbook
        {
            get => skillbook;
            private set => SetProperty(ref skillbook, value);
        }

        public Spellbook Spellbook
        {
            get => spellbook;
            private set => SetProperty(ref spellbook, value);
        }

        public PlayerStats Stats
        {
            get => stats;
            private set => SetProperty(ref stats, value);
        }

        public PlayerModifiers Modifiers
        {
            get => modifiers;
            private set => SetProperty(ref modifiers, value);
        }

        public MapLocation Location
        {
            get => location;
            private set => SetProperty(ref location, value);
        }

        public ClientState GameClient
        {
            get => gameClient;
            private set => SetProperty(ref gameClient, value);
        }

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

        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public bool IsMacroRunning
        {
            get => isMacroRunning;
            set => SetProperty(ref isMacroRunning, value);
        }

        public bool IsMacroPaused
        {
            get => isMacroPaused;
            set => SetProperty(ref isMacroPaused, value);
        }

        public bool IsMacroStopped
        {
            get => isMacroStopped;
            set => SetProperty(ref isMacroStopped, value);
        }

        public string HotkeyString => hotkey?.ToString();

        public Hotkey Hotkey
        {
            get => hotkey;
            set => SetProperty(ref hotkey, value, onChanged: (playerClass) => { RaisePropertyChanged(nameof(HotkeyString)); RaisePropertyChanged(nameof(HasHotkey)); });
        }

        public bool HasHotkey => !string.IsNullOrWhiteSpace(HotkeyString);

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set => SetProperty(ref selectedTabIndex, value);
        }

        public double? SkillbookScrollPosition
        {
            get => skillbookScrollPosition;
            set => SetProperty(ref skillbookScrollPosition, value);
        }

        public double? SpellbookScrollPosition
        {
            get => spellbookScrollPosition;
            set => SetProperty(ref spellbookScrollPosition, value);
        }

        public double? SpellQueueScrollPosition
        {
            get => spellQueueScrollPosition;
            set => SetProperty(ref spellQueueScrollPosition, value);
        }

        public double? FlowerScrollPosition
        {
            get => flowerScrollPosition;
            set => SetProperty(ref flowerScrollPosition, value);
        }

        public bool HasLyliacPlant
        {
            get => hasLyliacPlant;
            set => SetProperty(ref hasLyliacPlant, value);
        }

        public bool HasLyliacVineyard
        {
            get => hasLyliacVineyard;
            set => SetProperty(ref hasLyliacVineyard, value);
        }

        public bool HasFasSpiorad
        {
            get => hasFasSpiorad;
            set => SetProperty(ref hasFasSpiorad, value);
        }

        public DateTime LastFlowerTimestamp
        {
            get => lastFlowerTimestamp;
            set => SetProperty(ref lastFlowerTimestamp, value, onChanged: (p) => { RaisePropertyChanged(nameof(TimeSinceFlower)); });
        }

        public TimeSpan TimeSinceFlower => DateTime.Now - lastFlowerTimestamp;

        public Player(ClientProcess process)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            accessor = new ProcessMemoryAccessor(process.ProcessId, ProcessAccess.Read);

            if (NativeMethods.GetProcessTimes(accessor.ProcessHandle, out var creationTime, out _, out _, out _))
                process.CreationTime = creationTime.FiletimeToDateTime();

            inventory = new Inventory(this);
            equipment = new EquipmentSet(this);
            skillbook = new Skillbook(this);
            spellbook = new Spellbook(this);
            stats = new PlayerStats(this);
            modifiers = new PlayerModifiers(this);
            location = new MapLocation(this);
            gameClient = new ClientState(this);
        }

        ~Player() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                skillbook?.Dispose();
                accessor?.Dispose();
            }

            isDisposed = true;
        }

        public void Update(PlayerFieldFlags updateFields = PlayerFieldFlags.All)
        {
            GameClient.VersionKey = Version?.Key ?? "Unknown";

            var wasLoggedIn = IsLoggedIn;

            try
            {
                if (updateFields.HasFlag(PlayerFieldFlags.Name))
                    UpdateName(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Guild))
                    UpdateGuild(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.GuildRank))
                    UpdateGuildRank(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Title))
                    UpdateTitle(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Inventory))
                    inventory.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Equipment))
                    equipment.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Skillbook))
                    skillbook.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Spellbook))
                    spellbook.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Stats))
                    stats.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Modifiers))
                    modifiers.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Location))
                    location.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.GameClient))
                    gameClient.Update(accessor);

                if (updateFields.HasFlag(PlayerFieldFlags.Window))
                    Process.Update();
            }
            catch { }
            finally
            {
                IsLoggedIn = !string.IsNullOrWhiteSpace(Name) && stats.Level > 0;
            }

            var isNowLoggedIn = IsLoggedIn;

            if (isNowLoggedIn && !wasLoggedIn)
                OnLoggedIn();
            else if (wasLoggedIn && !isNowLoggedIn)
                OnLoggedOut();

            PlayerUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateName(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            string name = null;

            if (version != null && version.ContainsVariable(CharacterNameKey))
            {
                using var stream = accessor.GetStream();
                using var reader = new BinaryReader(stream, Encoding.ASCII);

                var nameVariable = version.GetVariable(CharacterNameKey);
                nameVariable.TryReadString(reader, out name);
            }

            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
        }

        private void UpdateGuild(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            // Not currently implemented
        }

        private void UpdateGuildRank(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            // Not currently implemented
        }

        private void UpdateTitle(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            // Not currently implemented
        }

        private void OnLoggedIn()
        {
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }

        void OnLoggedOut()
        {
            // This memory gets re-allocated when a new character logs into the same client instance
            skillbook.ResetCooldownPointer();

            LoggedOut?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString() => Name ?? string.Format("Process {0}", Process.ProcessId.ToString());
    }
}
