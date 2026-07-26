using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SleepHunter.Common;
using SleepHunter.Extensions;
using SleepHunter.IO.Process;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Settings;
using SleepHunter.Win32;

namespace SleepHunter.Models
{
    public sealed class Skillbook : UpdatableObject, IEnumerable<Skill>, IDisposable
    {
        private const string SkillbookKey = @"Skillbook";
        private const string SkillbookPanesKey = @"SkillbookPanes";
        private const string SkillbookPaneCapacityKey = @"SkillbookPaneCapacity";
        private const string SkillCooldownsKey = "SkillCooldowns";
        private const int SkillPaneSnapshotSize = 0x1B8;

        public const int TemuairSkillCount = 36;
        public const int MedeniaSkillCount = 36;
        public const int WorldSkillCount = 18;

        private readonly Skill[] skills = new Skill[TemuairSkillCount + MedeniaSkillCount + WorldSkillCount];
        private readonly ConcurrentDictionary<string, bool> activeSkills = new(StringComparer.OrdinalIgnoreCase);

        private readonly ProcessMemoryScanner scanner;
        private readonly Stream stream;
        private readonly BinaryReader reader;

        private nint baseCooldownPointer;

        public Player Owner { get; init; }

        public IEnumerable<Skill> AllSkills =>
            from s in skills select s;

        public IEnumerable<Skill> TemuairSkills =>
            from s in skills where s.Panel == InterfacePanel.TemuairSkills && s.Slot <= TemuairSkillCount select s;

        public IEnumerable<Skill> MedeniaSkills =>
            from s in skills where s.Panel == InterfacePanel.MedeniaSkills && s.Slot <= (TemuairSkillCount + MedeniaSkillCount) select s;

        public IEnumerable<Skill> WorldSkills =>
            from s in skills where s.Panel == InterfacePanel.WorldSkills && s.Slot <= (TemuairSkillCount + MedeniaSkillCount + WorldSkillCount) select s;

        public IEnumerable<string> ActiveSkills =>
            from a in activeSkills where a.Value select a.Key;

        public Skillbook(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            scanner = new ProcessMemoryScanner(Owner.ProcessHandle, leaveOpen: true);

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);

            for (var i = 0; i < skills.Length; i++)
                skills[i] = Skill.MakeEmpty(i + 1);
        }

        ~Skillbook() => Dispose(false);

        public Skill GetSkill(string skillName)
        {
            CheckIfDisposed();
            return skills.FirstOrDefault(skill => string.Equals(skill.Name, skillName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public bool? IsActive(string skillName)
        {
            CheckIfDisposed();

            if (skillName == null)
                return null;

            skillName = skillName.Trim();

            if (activeSkills.TryGetValue(skillName, out var activeState))
                return activeState;

            return null;
        }

        public bool? ToggleActive(string skillName, bool? isActive = null)
        {
            CheckIfDisposed();

            skillName = skillName.Trim();

            var hasPrevious =
                activeSkills.TryGetValue(
                    skillName,
                    out var previous);
            bool? wasActive = hasPrevious
                ? previous
                : null;
            activeSkills[skillName] =
                isActive ?? !previous;

            return wasActive;
        }

        public void ClearActiveSkills() => activeSkills.Clear();

        public void ResetCooldownPointer() => baseCooldownPointer = nint.Zero;

        protected override void OnUpdate()
        {
            var layout = Owner.Layout;

            if (layout == null)
            {
                ResetDefaults();
                return;
            }

            if (TryUpdateFromPanes(layout))
                return;

            if (!layout.TryGetVariable(SkillbookKey, out var skillbookVariable))
            {
                ResetDefaults();
                return;
            }

            if (!skillbookVariable.TryDereferenceValue(reader, out var basePointer))
            {
                ResetDefaults();
                return;
            }

            stream.Position = basePointer;

            var entryCount = Math.Min(skills.Length, skillbookVariable.Count);

            for (var i = 0; i < entryCount; i++)
            {
                SkillMetadata metadata = null;

                try
                {
                    var hasSkill = reader.ReadInt16() != 0;
                    var iconIndex = reader.ReadUInt16();
                    var rawName = reader.ReadFixedString(skillbookVariable.MaxLength);
                    var name = rawName;

                    if (!Ability.TryParseLevels(rawName, out name, out var currentLevel, out var maximumLevel))
                    {
                        name = rawName.Trim();
                        currentLevel = 0;
                        maximumLevel = 0;
                    }

                    skills[i].IsEmpty = !hasSkill;
                    skills[i].IconIndex = iconIndex;
                    skills[i].Icon = IconManager.Instance.GetSkillIcon(iconIndex);
                    skills[i].Name = name;
                    skills[i].CurrentLevel = currentLevel;
                    skills[i].MaximumLevel = maximumLevel;
                    ResetClientPaneState(skills[i]);

                    if (!skills[i].IsEmpty && !string.IsNullOrWhiteSpace(skills[i].Name))
                        metadata = SkillMetadataManager.Instance.GetSkill(name);

                    var isActive = IsActive(skills[i].Name);
                    skills[i].IsActive = isActive.HasValue && isActive.Value;

                    if (metadata != null)
                    {
                        skills[i].Cooldown = metadata.Cooldown;
                        skills[i].ManaCost = metadata.ManaCost;
                        skills[i].CanImprove = metadata.CanImprove;
                        skills[i].IsAssail = metadata.IsAssail;
                        skills[i].OpensDialog = metadata.OpensDialog;
                        skills[i].RequiresDisarm = metadata.RequiresDisarm;
                        skills[i].MinHealthPercent = metadata.MinHealthPercent > 0 ? metadata.MinHealthPercent : null;
                        skills[i].MaxHealthPercent = metadata.MaxHealthPercent > 0 ? metadata.MaxHealthPercent : null;
                    }
                    else
                    {
                        skills[i].Cooldown = TimeSpan.Zero;
                        skills[i].ManaCost = 0;
                        skills[i].CanImprove = true;
                        skills[i].IsAssail = false;
                        skills[i].OpensDialog = false;
                        skills[i].RequiresDisarm = false;
                        skills[i].MinHealthPercent = null;
                        skills[i].MaxHealthPercent = null;
                    }

                    skills[i].IsOnCooldown = IsSkillOnCooldown(
                        i,
                        layout,
                        reader,
                        Owner.Accessor.ProcessHandle);
                }
                catch { }
            }

            for (var i = entryCount; i < skills.Length; i++)
                ResetSkill(skills[i]);
        }

        private bool TryUpdateFromPanes(ClientLayout layout)
        {
            if (!layout.TryGetVariable(SkillbookPanesKey, out var panesVariable) ||
                !layout.TryGetVariable(SkillbookPaneCapacityKey, out var capacityVariable) ||
                !capacityVariable.TryReadInt32(reader, out var capacity) ||
                capacity <= 0 ||
                capacity > skills.Length ||
                !panesVariable.TryDereferenceValue(reader, out var panePointersAddress))
            {
                return false;
            }

            var pointerCount = Math.Min(capacity, panesVariable.Count);
            if (!RuntimeMemoryReader.TryReadBytes(
                reader,
                panePointersAddress,
                checked(pointerCount * sizeof(uint)),
                out var pointers))
            {
                return false;
            }

            var records = new SkillPaneRecord?[skills.Length];
            var populatedPointerCount = 0;
            for (var index = 0; index < pointerCount; index++)
            {
                var paneAddress = BinaryPrimitives.ReadUInt32LittleEndian(
                    pointers.AsSpan(index * sizeof(uint), sizeof(uint)));
                if (paneAddress == 0)
                    continue;

                populatedPointerCount++;
                if (!RuntimeMemoryReader.TryReadBytes(
                    reader,
                    paneAddress + 0x190,
                    SkillPaneSnapshotSize,
                    out var snapshot))
                {
                    return false;
                }

                var record = ParseSkillPaneSnapshot(snapshot);
                if (record.Slot == 0 || record.Slot > skills.Length || records[record.Slot - 1].HasValue)
                    return false;

                records[record.Slot - 1] = record;
            }

            if (populatedPointerCount == 0)
                return false;

            if (!capacityVariable.TryReadInt32(reader, out var currentCapacity) ||
                currentCapacity != capacity ||
                !panesVariable.TryDereferenceValue(reader, out var currentPanePointersAddress) ||
                currentPanePointersAddress != panePointersAddress)
            {
                return false;
            }

            for (var index = 0; index < skills.Length; index++)
            {
                if (!records[index].HasValue)
                {
                    ResetSkill(skills[index]);
                    continue;
                }

                var record = records[index].Value;
                var skill = skills[index];
                ParsePaneAbilityName(
                    record.Name,
                    record.NameSuffixLeft,
                    record.BaseNameLength,
                    out var name,
                    out var currentLevel,
                    out var maximumLevel);

                skill.IsEmpty = string.IsNullOrWhiteSpace(name);
                skill.IconIndex = record.IconIndex;
                skill.Icon = skill.IsEmpty ? null : IconManager.Instance.GetSkillIcon(record.IconIndex);
                skill.Name = name;
                skill.CurrentLevel = currentLevel;
                skill.MaximumLevel = maximumLevel;
                skill.CooldownProgress = record.CooldownProgress;
                skill.HasClientCooldownProgress = true;
                skill.CooldownStartMilliseconds = record.CooldownStartMilliseconds;
                skill.CooldownEndMilliseconds = record.CooldownEndMilliseconds;
                skill.IsOnCooldown = record.IsCooldownActive;
                skill.IsActionDelayed = record.ActionDelayActive;
                skill.ClientNameSuffixLeft = record.NameSuffixLeft;
                skill.ClientNameSuffixRight = record.NameSuffixRight;
                skill.ClientBaseNameLength = record.BaseNameLength;

                var isActive = IsActive(skill.Name);
                skill.IsActive = isActive.HasValue && isActive.Value;
                ApplyMetadata(skill);
            }

            return true;
        }

        internal static SkillPaneRecord ParseSkillPaneSnapshot(ReadOnlySpan<byte> snapshot)
        {
            if (snapshot.Length != SkillPaneSnapshotSize)
                throw new InvalidDataException(
                    $"A skill pane snapshot must contain {SkillPaneSnapshotSize} bytes.");

            return new SkillPaneRecord(
                BinaryPrimitives.ReadUInt16LittleEndian(snapshot),
                ReadNullTerminatedAscii(snapshot.Slice(0x02, 0x80)),
                snapshot[0x182],
                BinaryPrimitives.ReadUInt32LittleEndian(snapshot.Slice(0x184, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(snapshot.Slice(0x188, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(snapshot.Slice(0x18C, 4)),
                snapshot[0x190] != 0,
                snapshot[0x192] != 0,
                BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x1AC, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x1B0, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x1B4, 4)));
        }

        internal readonly record struct SkillPaneRecord(
            ushort IconIndex,
            string Name,
            byte Slot,
            uint CooldownProgress,
            uint CooldownStartMilliseconds,
            uint CooldownEndMilliseconds,
            bool CooldownVisualActive,
            bool ActionDelayActive,
            int NameSuffixLeft,
            int NameSuffixRight,
            int BaseNameLength)
        {
            // The progress counter is retained after the owning timer clears. The
            // separate visual-active byte is the client's cooldown lifecycle flag.
            public bool IsCooldownActive => CooldownVisualActive;
        }

        private static string ReadNullTerminatedAscii(ReadOnlySpan<byte> bytes)
        {
            var terminator = bytes.IndexOf((byte)0);
            if (terminator >= 0)
                bytes = bytes[..terminator];

            return Encoding.ASCII.GetString(bytes);
        }

        private static void ParsePaneAbilityName(
            string rawName,
            int suffixLeft,
            int baseNameLength,
            out string name,
            out int currentLevel,
            out int maximumLevel)
        {
            if (Ability.TryParseLevels(rawName, out name, out currentLevel, out maximumLevel))
                return;

            name = rawName?.Trim();
            currentLevel = 0;
            maximumLevel = 0;

            if (baseNameLength > 0 && rawName != null && baseNameLength <= rawName.Length)
            {
                name = rawName[..baseNameLength].Trim();
                if (suffixLeft > 0)
                    currentLevel = suffixLeft;
            }
        }

        private void ApplyMetadata(Skill skill)
        {
            var metadata = !skill.IsEmpty && !string.IsNullOrWhiteSpace(skill.Name)
                ? SkillMetadataManager.Instance.GetSkill(skill.Name)
                : null;

            if (metadata != null)
            {
                skill.Cooldown = metadata.Cooldown;
                skill.ManaCost = metadata.ManaCost;
                skill.CanImprove = metadata.CanImprove;
                skill.IsAssail = metadata.IsAssail;
                skill.OpensDialog = metadata.OpensDialog;
                skill.RequiresDisarm = metadata.RequiresDisarm;
                skill.MinHealthPercent = metadata.MinHealthPercent > 0 ? metadata.MinHealthPercent : null;
                skill.MaxHealthPercent = metadata.MaxHealthPercent > 0 ? metadata.MaxHealthPercent : null;
            }
            else
            {
                skill.Cooldown = TimeSpan.Zero;
                skill.ManaCost = 0;
                skill.CanImprove = true;
                skill.IsAssail = false;
                skill.OpensDialog = false;
                skill.RequiresDisarm = false;
                skill.MinHealthPercent = null;
                skill.MaxHealthPercent = null;
            }
        }

        private static void ResetClientPaneState(Skill skill)
        {
            skill.CooldownProgress = 0;
            skill.HasClientCooldownProgress = false;
            skill.CooldownStartMilliseconds = 0;
            skill.CooldownEndMilliseconds = 0;
            skill.IsActionDelayed = false;
            skill.ClientNameSuffixLeft = 0;
            skill.ClientNameSuffixRight = 0;
            skill.ClientBaseNameLength = 0;
        }

        private static void ResetSkill(Skill skill)
        {
            skill.IsEmpty = true;
            skill.IconIndex = 0;
            skill.Icon = null;
            skill.Name = null;
            skill.CurrentLevel = 0;
            skill.MaximumLevel = 0;
            skill.IsActive = false;
            skill.IsOnCooldown = false;
            skill.Cooldown = TimeSpan.Zero;
            skill.ManaCost = 0;
            skill.CanImprove = true;
            skill.IsAssail = false;
            skill.OpensDialog = false;
            skill.RequiresDisarm = false;
            skill.MinHealthPercent = null;
            skill.MaxHealthPercent = null;
            ResetClientPaneState(skill);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                scanner?.Dispose();
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void ResetDefaults()
        {
            activeSkills.Clear();

            for (int i = 0; i < skills.Length; i++)
                ResetSkill(skills[i]);
        }

        private bool IsSkillOnCooldown(
            int slot,
            ClientLayout layout,
            BinaryReader reader,
            nint processHandle)
        {
            if (layout == null ||
                !UpdateSkillbookCooldownPointer(
                    layout,
                    reader,
                    processHandle))
                return false;

            if (!IsReadableMemory(processHandle, baseCooldownPointer))
                return false;

            long position = reader.BaseStream.Position;

            try
            {
                if (layout.GetVariable(SkillCooldownsKey) is not SearchMemoryVariable cooldownVariable)
                    return false;

                var offset = cooldownVariable.Offsets.FirstOrDefault();

                if (offset == null)
                    return false;

                var address = (long)baseCooldownPointer + (slot * cooldownVariable.Size);

                if (!IsReadableMemory(processHandle, address))
                    return false;

                reader.BaseStream.Position = address;
                address = reader.ReadUInt32();

                if (!IsReadableMemory(processHandle, address))
                    return false;

                if (offset.IsNegative)
                    address -= offset.Offset;
                else
                    address += offset.Offset;

                reader.BaseStream.Position = address;
                var cooldownFlag = reader.ReadByte();

                return cooldownFlag != 0x00;
            }
            catch
            {
                ResetCooldownPointer();
                return false;
            }
            finally { reader.BaseStream.Position = position; }
        }

        private bool UpdateSkillbookCooldownPointer(
            ClientLayout layout,
            BinaryReader reader,
            nint processHandle)
        {
            if (layout == null)
                return false;

            var position = reader.BaseStream.Position;

            try
            {
                if (layout.GetVariable(SkillCooldownsKey) is not SearchMemoryVariable cooldownVariable)
                    return false;

                if (baseCooldownPointer != nint.Zero)
                    return true;

                var ptrs = scanner.FindAllUInt32((uint)cooldownVariable.Address)
                    .Select(ptr =>
                    {
                        if (cooldownVariable.Offset.IsNegative)
                            ptr = (nint)((uint)ptr - (uint)cooldownVariable.Offset.Offset);
                        else
                            ptr = (nint)((uint)ptr + (uint)cooldownVariable.Offset.Offset);

                        return ptr;
                    })
                    .Where(ptr => IsReadableMemory(processHandle, ptr))
                    .ToList();


                foreach (var ptr in ptrs)
                {
                    if (ptr == nint.Zero)
                        continue;

                    reader.BaseStream.Position = ptr;
                    var cooldownPtr = reader.ReadUInt32();

                    if (cooldownPtr == 0 || !IsReadableMemory(processHandle, cooldownPtr))
                        continue;

                    baseCooldownPointer = (nint)cooldownPtr;
                    return true;
                }

                return false;
            }
            catch { baseCooldownPointer = nint.Zero; return false; }
            finally { reader.BaseStream.Position = position; }
        }

        private static bool IsReadableMemory(nint processHandle, long address)
        {
            if (address <= 0)
                return false;

            var sizeOfMemoryInfo = Marshal.SizeOf(typeof(MemoryBasicInformation));
            var byteCount = (int)NativeMethods.VirtualQueryEx(processHandle, (nint)address, out var memoryInfo, sizeOfMemoryInfo);

            if (byteCount <= 0)
                return false;

            if (memoryInfo.Type != VirtualMemoryType.Private)
                return false;

            if (memoryInfo.State == VirtualMemoryStatus.Free)
                return false;

            return true;
        }

        public IEnumerator<Skill> GetEnumerator()
        {
            foreach (var skill in skills)
                if (!skill.IsEmpty)
                    yield return skill;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
