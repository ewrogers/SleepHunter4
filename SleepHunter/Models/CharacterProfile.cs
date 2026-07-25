using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.Common;
using SleepHunter.Extensions;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class CharacterProfile : UpdatableObject
    {
        private const string CharacterClassKey = @"CharacterClass";
        private const string CharacterIdKey = @"CharacterId";
        private const string UserStateKey = @"UserState";
        private const string PrivilegeLevelKey = @"PrivilegeLevel";
        private const string ActionStateKey = @"ActionState";
        private const string NationKey = @"Nation";
        private const string TitleKey = @"Title";
        private const string DisplayClassKey = @"DisplayClass";
        private const string GuildKey = @"Guild";
        private const string GuildRankKey = @"GuildRank";
        private const string GroupMembersKey = @"GroupMembers";
        private const string GroupMemberCacheKey = @"GroupMemberCache";
        private const string GroupMemberCountKey = @"GroupMemberCount";
        private const string ShowAbilityMetadataKey = @"ShowAbilityMetadata";
        private const string ShowMasterMetadataKey = @"ShowMasterMetadata";

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private PlayerClass? characterClass;
        private uint characterId;
        private PlayerUserState? userState;
        private int privilegeLevel;
        private byte actionState;
        private byte nation;
        private string title;
        private string displayClass;
        private string guild;
        private string guildRank;
        private string groupMembers;
        private IReadOnlyList<GroupMember> groupMemberEntries = Array.Empty<GroupMember>();
        private IReadOnlyList<string> groupMemberNames = Array.Empty<string>();
        private bool showAbilityMetadata;
        private bool showMasterMetadata;

        public Player Owner { get; init; }

        public PlayerClass? CharacterClass
        {
            get => characterClass;
            set => SetProperty(ref characterClass, value);
        }

        public uint CharacterId
        {
            get => characterId;
            set => SetProperty(ref characterId, value);
        }

        public PlayerUserState? UserState
        {
            get => userState;
            set => SetProperty(ref userState, value);
        }

        public int PrivilegeLevel
        {
            get => privilegeLevel;
            set => SetProperty(ref privilegeLevel, value);
        }

        public byte ActionState
        {
            get => actionState;
            set => SetProperty(ref actionState, value, onChanged: (_) => RaisePropertyChanged(nameof(IsActionLocked)));
        }

        public bool IsActionLocked => (ActionState & 0x01) != 0;

        public byte Nation
        {
            get => nation;
            set => SetProperty(ref nation, value);
        }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public string DisplayClass
        {
            get => displayClass;
            set => SetProperty(ref displayClass, value);
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

        public string GroupMembers
        {
            get => groupMembers;
            set => SetProperty(ref groupMembers, value);
        }

        public IReadOnlyList<string> GroupMemberNames
        {
            get => groupMemberNames;
            private set => SetProperty(ref groupMemberNames, value);
        }

        public IReadOnlyList<GroupMember> GroupMemberEntries
        {
            get => groupMemberEntries;
            private set => SetProperty(ref groupMemberEntries, value);
        }

        public bool ShowAbilityMetadata
        {
            get => showAbilityMetadata;
            set => SetProperty(ref showAbilityMetadata, value);
        }

        public bool ShowMasterMetadata
        {
            get => showMasterMetadata;
            set => SetProperty(ref showMasterMetadata, value);
        }

        public CharacterProfile(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }

        protected override void OnUpdate()
        {
            var version = Owner.Version;
            if (version == null)
            {
                ResetDefaults();
                return;
            }

            CharacterClass = ReadCharacterClass(version);
            CharacterId = (uint)ReadInteger(version, CharacterIdKey);
            UserState = ReadUserState(version);
            PrivilegeLevel = (int)ReadInteger(version, PrivilegeLevelKey);
            ActionState = (byte)ReadInteger(version, ActionStateKey);
            Nation = (byte)ReadInteger(version, NationKey);
            Title = ReadString(version, TitleKey);
            DisplayClass = ReadString(version, DisplayClassKey);
            Guild = ReadString(version, GuildKey);
            GuildRank = ReadString(version, GuildRankKey);
            GroupMembers = ReadString(version, GroupMembersKey);
            GroupMemberEntries = ReadGroupMemberEntries(version);
            GroupMemberNames = GroupMemberEntries.Select(member => member.Name).ToArray();
            ShowAbilityMetadata = ReadInteger(version, ShowAbilityMetadataKey) != 0;
            ShowMasterMetadata = ReadInteger(version, ShowMasterMetadataKey) != 0;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private PlayerClass? ReadCharacterClass(Settings.ClientVersion version)
        {
            var rawValue = ReadInteger(version, CharacterClassKey, out var wasRead);
            if (!wasRead || rawValue < byte.MinValue || rawValue > byte.MaxValue)
                return null;

            return PlayerClassExtensions.TryFromClientValue((byte)rawValue, out var parsedClass)
                ? parsedClass
                : null;
        }

        private PlayerUserState? ReadUserState(Settings.ClientVersion version)
        {
            var rawValue = ReadInteger(version, UserStateKey, out var wasRead);
            if (!wasRead)
                return null;

            rawValue &= 0xFF;
            return rawValue <= (byte)PlayerUserState.NeedHelp
                ? (PlayerUserState)rawValue
                : null;
        }

        private long ReadInteger(Settings.ClientVersion version, string key) =>
            ReadInteger(version, key, out _);

        private long ReadInteger(Settings.ClientVersion version, string key, out bool wasRead)
        {
            wasRead = false;

            var variable = version.GetVariable(key);
            if (variable == null || !variable.TryReadInteger(reader, out var value))
                return 0;

            wasRead = true;
            return value;
        }

        private string ReadString(Settings.ClientVersion version, string key)
        {
            var variable = version.GetVariable(key);
            return variable != null && variable.TryReadString(reader, out var value)
                ? value
                : null;
        }

        private IReadOnlyList<GroupMember> ReadGroupMemberEntries(Settings.ClientVersion version)
        {
            var countValue = ReadInteger(version, GroupMemberCountKey, out var countWasRead);
            if (!countWasRead || countValue <= 0)
                return Array.Empty<GroupMember>();

            var count = (int)Math.Min(countValue, 64);
            if (!version.TryGetVariable(GroupMemberCacheKey, out var cacheVariable) ||
                !cacheVariable.TryDereferenceValue(reader, out var cacheAddress) ||
                cacheVariable.Size < 65 ||
                !RuntimeMemoryReader.TryReadBytes(reader, cacheAddress, checked(count * cacheVariable.Size), out var snapshot))
            {
                return Array.Empty<GroupMember>();
            }

            var members = new List<GroupMember>(count);
            for (var index = 0; index < count; index++)
            {
                var record = snapshot.AsSpan(index * cacheVariable.Size, cacheVariable.Size);
                var nameBytes = record[..Math.Min(cacheVariable.MaxLength, record.Length)];
                var terminator = nameBytes.IndexOf((byte)0);
                if (terminator >= 0)
                    nameBytes = nameBytes[..terminator];

                var name = Encoding.ASCII.GetString(nameBytes).Trim();
                if (IsValidGroupMemberName(name))
                    members.Add(new GroupMember(name, record[0x40] != 0));
            }

            return members
                .DistinctBy(member => member.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        internal static bool IsValidGroupMemberName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 12 || !IsAsciiLetter(name[0]))
                return false;

            return name.All(character =>
                IsAsciiLetter(character) ||
                character is >= '0' and <= '9' ||
                character == '-');
        }

        private static bool IsAsciiLetter(char character) =>
            character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

        private void ResetDefaults()
        {
            CharacterClass = null;
            CharacterId = 0;
            UserState = null;
            PrivilegeLevel = 0;
            ActionState = 0;
            Nation = 0;
            Title = null;
            DisplayClass = null;
            Guild = null;
            GuildRank = null;
            GroupMembers = null;
            GroupMemberEntries = Array.Empty<GroupMember>();
            GroupMemberNames = Array.Empty<string>();
            ShowAbilityMetadata = false;
            ShowMasterMetadata = false;
        }
    }
}
