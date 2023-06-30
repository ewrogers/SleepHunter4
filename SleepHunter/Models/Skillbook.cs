using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.Common;
using SleepHunter.Extensions;
using SleepHunter.IO.Process;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Settings;
using SleepHunter.Win32;
using System.Runtime.InteropServices;

namespace SleepHunter.Models
{
    public sealed class Skillbook : UpdatableObject, IEnumerable<Skill>, IDisposable
    {
        private const string SkillbookKey = @"Skillbook";
        private const string SkillCooldownsKey = "SkillCooldowns";

        public const int TemuairSkillCount = 36;
        public const int MedeniaSkillCount = 36;
        public const int WorldSkillCount = 18;

        private readonly Skill[] skills = new Skill[TemuairSkillCount + MedeniaSkillCount + WorldSkillCount];
        private readonly ConcurrentDictionary<string, bool> activeSkills = new(StringComparer.OrdinalIgnoreCase);

        private readonly ProcessMemoryScanner scanner;
        private readonly Stream stream;
        private readonly BinaryReader reader;

        private nint baseCooldownPointer;

        public Player Owner { get; init;  }

        public IEnumerable<Skill> AllSkills => 
            from s in skills select s;

        public IEnumerable<Skill> TemuairSkills => 
            from s in skills where s.Panel == InterfacePanel.TemuairSkills && s.Slot < TemuairSkillCount select s;

        public IEnumerable<Skill> MedeniaSkills => 
            from s in skills where s.Panel == InterfacePanel.MedeniaSkills && s.Slot < (TemuairSkillCount + MedeniaSkillCount) select s;

        public IEnumerable<Skill> WorldSkills => 
            from s in skills where s.Panel == InterfacePanel.WorldSkills && s.Slot < (TemuairSkillCount + MedeniaSkillCount + WorldSkillCount) select s;

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

        public bool ContainSkill(string skillName)
        {
            CheckIfDisposed();
            return skills.Any(skill => string.Equals(skill.Name, skillName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

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

            bool? wasActive = null;

            if (activeSkills.ContainsKey(skillName))
            {
                wasActive = activeSkills[skillName];
                activeSkills[skillName] = !wasActive.Value;
            }
            else
            {
                activeSkills[skillName] = true;
            }

            return wasActive;
        }

        public void ClearActiveSkills() => activeSkills.Clear();

        public void ResetCooldownPointer() => baseCooldownPointer = nint.Zero;

        protected override void OnUpdate()
        {
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            if (!version.TryGetVariable(SkillbookKey, out var skillbookVariable))
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
                    var name = reader.ReadFixedString(skillbookVariable.MaxLength);

                    if (!Ability.TryParseLevels(name, out name, out var currentLevel, out var maximumLevel))
                    {
                        if (!string.IsNullOrWhiteSpace(name))
                            skills[i].Name = name.Trim();
                    }

                    skills[i].IsEmpty = !hasSkill;
                    skills[i].IconIndex = iconIndex;
                    skills[i].Icon = IconManager.Instance.GetSkillIcon(iconIndex);
                    skills[i].Name = name;
                    skills[i].CurrentLevel = currentLevel;
                    skills[i].MaximumLevel = maximumLevel;

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

                    skills[i].IsOnCooldown = IsSkillOnCooldown(i, version, reader, Owner.Accessor.ProcessHandle);
                }
                catch { }
            }
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
            {
                skills[i].IsEmpty = true;
                skills[i].Name = null;
            }
        }

        private bool IsSkillOnCooldown(int slot, ClientVersion version, BinaryReader reader, nint processHandle)
        {
            if (version == null || !UpdateSkillbookCooldownPointer(version, reader, processHandle))
                return false;

            if (!IsReadableMemory(processHandle, baseCooldownPointer))
                return false;

            long position = reader.BaseStream.Position;

            try
            {
                if (version.GetVariable(SkillCooldownsKey) is not SearchMemoryVariable cooldownVariable)
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

        private bool UpdateSkillbookCooldownPointer(ClientVersion version, BinaryReader reader, nint processHandle)
        {
            if (version == null)
                return false;

            var position = reader.BaseStream.Position;

            try
            {
                if (version.GetVariable(SkillCooldownsKey) is not SearchMemoryVariable cooldownVariable)
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
