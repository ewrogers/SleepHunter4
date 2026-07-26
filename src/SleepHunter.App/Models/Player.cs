using System;
using System.IO;
using System.Text;
using SleepHunter.Common;
using SleepHunter.IO.Process;
using SleepHunter.Macro;
using SleepHunter.Settings;

namespace SleepHunter.Models
{
    public sealed class Player : UpdatableObject, IDisposable
    {
        private const string CharacterNameKey = @"CharacterName";
        private const string WorldUserFuncKey = @"WorldUserFunc";

        private readonly ProcessMemoryAccessor accessor;
        private readonly ClientState gameClient;
        private readonly Inventory inventory;
        private readonly EquipmentSet equipment;
        private readonly Skillbook skillbook;
        private readonly Spellbook spellbook;
        private readonly PlayerStats stats;
        private readonly CharacterProfile profile;
        private readonly PlayerModifiers modifiers;
        private readonly MapLocation location;

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private ClientLayout layout;
        private long nameSessionAddress;

        private string name;
        private DateTime? loginTimestamp;
        private bool isLoggedIn;
        private string status;
        private Hotkey hotkey;
        private int selectedTabIndex;
        private bool hasLyliacPlant;
        private bool hasLyliacVineyard;

        public event EventHandler LoggedIn;
        public event EventHandler LoggedOut;

        public ClientProcess Process { get; init; }

        public ClientLayout Layout
        {
            get => layout;
            set => SetProperty(ref layout, value);
        }

        public nint ProcessHandle => accessor.ProcessHandle;

        public ProcessMemoryAccessor Accessor => accessor;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public ClientState GameClient => gameClient;

        public Inventory Inventory => inventory;

        public EquipmentSet Equipment => equipment;

        public Skillbook Skillbook => skillbook;

        public Spellbook Spellbook => spellbook;

        public PlayerStats Stats => stats;

        public CharacterProfile Profile => profile;

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

        public Player(ClientProcess process)
        {
            Process = process ?? throw new ArgumentNullException(nameof(process));
            accessor = new ProcessMemoryAccessor(process.ProcessId, ProcessAccess.Read);

            stream = accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);

            gameClient = new ClientState(this);
            inventory = new Inventory(this);
            equipment = new EquipmentSet(this);
            skillbook = new Skillbook(this);
            spellbook = new Spellbook(this);
            stats = new PlayerStats(this);
            profile = new CharacterProfile(this);
            modifiers = new PlayerModifiers(this);
            location = new MapLocation(this);
        }

        ~Player() => Dispose(false);

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                gameClient.Dispose();
                inventory.Dispose();
                equipment.Dispose();
                skillbook.Dispose();
                spellbook.Dispose();
                stats.Dispose();
                profile.Dispose();
                modifiers.Dispose();
                location.Dispose();

                stream.Dispose();
                reader.Dispose();
                accessor.Dispose();
            }

            base.Dispose(isDisposing);
        }

        protected override void OnUpdate()
        {
            Process.TryUpdate();
            gameClient.TryUpdate();

            try
            {
                UpdateName(accessor);
            }
            catch { }

            stats.TryUpdate();
            profile.TryUpdate();
            modifiers.TryUpdate();
            location.TryUpdate();
            inventory.TryUpdate();
            equipment.TryUpdate();
            skillbook.TryUpdate();
            spellbook.TryUpdate();

            var wasLoggedIn = IsLoggedIn;
            var isNowLoggedIn = !string.IsNullOrWhiteSpace(Name) && stats.Level > 0;

            if (isNowLoggedIn && !wasLoggedIn)
                OnLoggedIn();
            else if (wasLoggedIn && !isNowLoggedIn)
                OnLoggedOut();
        }

        private void UpdateName(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            if (layout == null)
            {
                ClearCharacterNameSession();
                return;
            }

            if (layout.TryGetVariable(WorldUserFuncKey, out var sessionVariable))
            {
                if (!sessionVariable.TryDereferenceValue(reader, out var sessionAddress))
                {
                    ClearCharacterNameSession();
                    return;
                }

                if (nameSessionAddress != sessionAddress)
                {
                    Name = null;
                    nameSessionAddress = sessionAddress;
                }
            }

            if (!layout.TryGetVariable(CharacterNameKey, out var nameVariable))
                return;

            string candidateName;
            if (nameVariable is DynamicMemoryVariable)
            {
                if (!nameVariable.TryReadString(reader, out candidateName))
                    return;
            }
            else
            {
                var nameAddress = nameVariable.DereferenceValue(reader);
                if (!RuntimeMemoryReader.TryReadAsciiString(
                    reader,
                    nameAddress,
                    nameVariable.MaxLength,
                    out candidateName,
                    requireTerminator: true))
                {
                    return;
                }
            }

            if (IsValidCharacterName(candidateName))
                Name = candidateName;
        }

        internal static bool IsValidCharacterName(string candidateName) =>
            CharacterProfile.IsValidGroupMemberName(candidateName);

        private void ClearCharacterNameSession()
        {
            nameSessionAddress = 0;
            Name = null;
        }

        private void OnLoggedIn()
        {
            IsLoggedIn = true;
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }

        void OnLoggedOut()
        {
            // This memory gets re-allocated when a new character logs into the same client instance
            skillbook.ResetCooldownPointer();
            nameSessionAddress = 0;

            IsLoggedIn = false;
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString() => Name ?? string.Format("Process {0}", Process.ProcessId.ToString());
    }
}
