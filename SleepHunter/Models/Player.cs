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
    public sealed class Player : UpdatableObject, IDisposable
    {
        private const string CharacterNameKey = @"CharacterName";

        private readonly ProcessMemoryAccessor accessor;
        private readonly ClientState gameClient;
        private readonly Inventory inventory;
        private readonly EquipmentSet equipment;
        private readonly Skillbook skillbook;
        private readonly Spellbook spellbook;
        private readonly PlayerStats stats;
        private readonly PlayerModifiers modifiers;
        private readonly MapLocation location;
        
        private ClientVersion version;
        
        private string name;
        private string guild;
        private string guildRank;
        private string title;
        private PlayerClass playerClass;
        private DateTime? loginTimestamp;
        private bool isLoggedIn;
        private string status;
        private bool isMacroRunning;
        private bool isMacroPaused;
        private bool isMacroStopped;
        private Hotkey hotkey;
        private int selectedTabIndex;
        private bool hasLyliacPlant;
        private bool hasLyliacVineyard;
        private bool hasFasSpiorad;
        private DateTime lastFlowerTimestamp;

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

        public ClientState GameClient => gameClient;

        public Inventory Inventory => inventory;

        public EquipmentSet Equipment => equipment;

        public Skillbook Skillbook => skillbook;

        public Spellbook Spellbook => spellbook;

        public PlayerStats Stats => stats;

        public PlayerModifiers Modifiers => modifiers;

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


        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                inventory.Dispose();
                equipment.Dispose();

                skillbook?.Dispose();
                accessor?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        protected override void OnUpdate()
        {

            GameClient.VersionKey = Version?.Key ?? "Unknown";

            try
            {
                gameClient.Update(accessor);
            }
            catch { }

            try
            {
                Process.Update();
            }
            catch { }

            try
            {
                UpdateName(accessor);
            }
            catch { }

            try
            {
                UpdateGuild(accessor);
            }
            catch { }

            try
            {
                UpdateGuildRank(accessor);
            }
            catch { }

            try
            {
                UpdateTitle(accessor);
            }
            catch { }

            try
            {
                stats.Update(accessor);
            }
            catch { }

            try
            {
                modifiers.Update(accessor);
            }
            catch { }

            try
            {
                location.Update(accessor);
            }
            catch { }

            inventory.TryUpdate();

            equipment.TryUpdate();

            try
            {
                skillbook.Update(accessor);
            }
            catch { }

            try
            {
                spellbook.Update(accessor);
            }
            catch { }

            var wasLoggedIn = IsLoggedIn;
            IsLoggedIn = !string.IsNullOrWhiteSpace(Name) && stats.Level > 0;
            var isNowLoggedIn = IsLoggedIn;

            if (isNowLoggedIn && !wasLoggedIn)
                OnLoggedIn();
            else if (wasLoggedIn && !isNowLoggedIn)
                OnLoggedOut();
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
