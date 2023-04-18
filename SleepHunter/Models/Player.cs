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
        private static readonly string CharacterNameKey = @"CharacterName";

        private bool isDisposed;
        private ClientProcess process;
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

        public ClientProcess Process
        {
            get => process;
            private set => SetProperty(ref process, value);
        }

        public ClientVersion Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        public IntPtr ProcessHandle => accessor.ProcessHandle;

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
            set => SetProperty(ref inventory, value);
        }

        public EquipmentSet Equipment
        {
            get => equipment;
            set => SetProperty(ref equipment, value);
        }

        public Skillbook Skillbook
        {
            get => skillbook;
            set => SetProperty(ref skillbook, value);
        }

        public Spellbook Spellbook
        {
            get => spellbook;
            set => SetProperty(ref spellbook, value);
        }

        public PlayerStats Stats
        {
            get => stats;
            set => SetProperty(ref stats, value);
        }

        public PlayerModifiers Modifiers
        {
            get => modifiers;
            set => SetProperty(ref modifiers, value);
        }

        public MapLocation Location
        {
            get => location;
            set => SetProperty(ref location, value);
        }

        public ClientState GameClient
        {
            get => gameClient;
            set => SetProperty(ref gameClient, value);
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

        public string HotkeyString => hotkey?.ToString();

        public Hotkey Hotkey
        {
            get => hotkey;
            set => SetProperty(ref hotkey, value, onChanged: (playerClass) =>
            {
                RaisePropertyChanged(nameof(HasHotkey));
            });
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
            set => SetProperty(ref lastFlowerTimestamp, value, onChanged: (p) =>
            {
                RaisePropertyChanged(nameof(TimeSinceFlower));
            });
        }

        public TimeSpan TimeSinceFlower => DateTime.Now - lastFlowerTimestamp;

        public Player(ClientProcess process)
        {
            this.process = process;
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

        private void Dispose(bool isDisposing)
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

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Update(PlayerFieldFlags updateFields = PlayerFieldFlags.All)
        {
            CheckIfDisposed();
            GameClient.VersionKey = Version?.Key ?? "Unknown";

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
                    process.Update();
            }
            catch { }
            finally
            {
                IsLoggedIn = !string.IsNullOrWhiteSpace(Name) && stats.Level > 0;
            }
        }

        private void UpdateName(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            string name = null;

            if (version != null && version.ContainsVariable(CharacterNameKey))
            {
                Stream stream = null;
                try
                {
                    stream = accessor.GetStream();
                    using (var reader = new BinaryReader(stream, Encoding.ASCII))
                    {
                        stream = null;

                        var nameVariable = version.GetVariable(CharacterNameKey);
                        nameVariable.TryReadString(reader, out name);
                    }
                }
                finally { stream?.Dispose(); }
            }

            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
        }

        private void UpdateGuild(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));
        }

        private void UpdateGuildRank(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));
        }

        private void UpdateTitle(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));
        }

        public override string ToString() => Name ?? $"Process {Process.ProcessId}";
    }
}
