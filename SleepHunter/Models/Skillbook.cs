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
using System.Diagnostics;

namespace SleepHunter.Models
{
    public sealed class Skillbook : ObservableObject, IEnumerable<Skill>, IDisposable
    {
        static readonly string SkillbookKey = @"Skillbook";
        static readonly string SkillCooldownsKey = "SkillCooldowns";

        public static readonly int TemuairSkillCount = 36;
        public static readonly int MedeniaSkillCount = 36;
        public static readonly int WorldSkillCount = 18;

        bool isDisposed;
        Player owner;
        List<Skill> skills = new List<Skill>(TemuairSkillCount + MedeniaSkillCount + WorldSkillCount);
        ConcurrentDictionary<string, bool> activeSkills = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        ProcessMemoryScanner scanner;
        IntPtr baseCooldownPointer;

        public event EventHandler SkillbookUpdated;

        public Player Owner
        {
            get { return owner; }
            set { SetProperty(ref owner, value); }
        }

        public int Count { get { return skills.Count((skill) => { return !skill.IsEmpty; }); } }

        public IEnumerable<Skill> Skills
        {
            get { return from s in skills select s; }
        }

        public IEnumerable<Skill> TemuairSkills
        {
            get { return from s in skills where s.Panel == InterfacePanel.TemuairSkills && s.Slot < TemuairSkillCount select s; }
        }

        public IEnumerable<Skill> MedeniaSkills
        {
            get { return from s in skills where s.Panel == InterfacePanel.MedeniaSkills && s.Slot < (TemuairSkillCount + MedeniaSkillCount) select s; }
        }

        public IEnumerable<Skill> WorldSkills
        {
            get { return from s in skills where s.Panel == InterfacePanel.WorldSkills && s.Slot < (TemuairSkillCount + MedeniaSkillCount + WorldSkillCount) select s; }
        }

        public IEnumerable<string> ActiveSkills
        {
            get { return from a in activeSkills where a.Value select a.Key; }
        }

        public Skillbook()
           : this(null) { }

        public Skillbook(Player owner)
        {
            this.owner = owner;
            scanner = new ProcessMemoryScanner(owner.ProcessHandle, leaveOpen: true);

            InitializeSkillbook();
        }

        #region IDisposable Methods
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
                if (scanner != null)
                    scanner.Dispose();
            }

            isDisposed = true;
        }
        #endregion

        void InitializeSkillbook()
        {
            skills.Clear();

            for (int i = 0; i < skills.Capacity; i++)
                skills.Add(Skill.MakeEmpty(i + 1));
        }

        public bool ContainSkill(string skillName)
        {
            skillName = skillName.Trim();

            foreach (var skill in skills)
                if (string.Equals(skill.Name, skillName, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public Skill GetSkill(string skillName)
        {
            skillName = skillName.Trim();

            foreach (var skill in skills)
                if (string.Equals(skill.Name, skillName, StringComparison.OrdinalIgnoreCase))
                    return skill;

            return null;
        }

        public bool? IsActive(string skillName)
        {
            if (skillName == null)
                return null;

            skillName = skillName.Trim();

            if (activeSkills.ContainsKey(skillName))
                return activeSkills[skillName];
            else
                return null;
        }

        public bool? ToggleActive(string skillName, bool? isActive = null)
        {
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

        public void ClearActiveSkills()
        {
            activeSkills.Clear();
        }

        public void ResetCooldownPointer()
        {
            baseCooldownPointer = IntPtr.Zero;
        }

        public void Update()
        {
            if (owner == null)
                throw new InvalidOperationException("Player owner is null, cannot update.");

            Update(owner.Accessor);
            SkillbookUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Update(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            var skillbookVariable = version.GetVariable(SkillbookKey);

            if (skillbookVariable == null)
            {
                ResetDefaults();
                return;
            }

            Stream stream = null;
            try
            {
                stream = accessor.GetStream();
                using (var reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    stream = null;
                    var skillbookPointer = skillbookVariable.DereferenceValue(reader);

                    if (skillbookPointer == 0)
                    {
                        ResetDefaults();
                        return;
                    }

                    reader.BaseStream.Position = skillbookPointer;

                    for (int i = 0; i < skillbookVariable.Count; i++)
                    {
                        SkillMetadata metadata = null;

                        try
                        {
                            bool hasSkill = reader.ReadInt16() != 0;
                            ushort iconIndex = reader.ReadUInt16();
                            string name = reader.ReadFixedString(skillbookVariable.MaxLength);

                            int currentLevel, maximumLevel;
                            if (!Ability.TryParseLevels(name, out name, out currentLevel, out maximumLevel))
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

                            var isActive = this.IsActive(skills[i].Name);
                            skills[i].IsActive = isActive.HasValue && isActive.Value;

                            if (metadata != null)
                            {
                                skills[i].Cooldown = metadata.Cooldown;
                                skills[i].ManaCost = metadata.ManaCost;
                                skills[i].CanImprove = metadata.CanImprove;
                                skills[i].IsAssail = metadata.IsAssail;
                                skills[i].OpensDialog = metadata.OpensDialog;
                                skills[i].RequiresDisarm = metadata.RequiresDisarm;
                            }
                            else
                            {
                                skills[i].Cooldown = TimeSpan.Zero;
                                skills[i].ManaCost = 0;
                                skills[i].CanImprove = true;
                                skills[i].IsAssail = false;
                                skills[i].OpensDialog = false;
                                skills[i].RequiresDisarm = false;
                            }

                            skills[i].IsOnCooldown = IsSkillOnCooldown(i, version, reader, accessor.ProcessHandle);
                        }
                        catch { }
                    }
                }
            }
            finally { stream?.Dispose(); }
        }

        public void ResetDefaults()
        {
            activeSkills.Clear();

            for (int i = 0; i < skills.Capacity; i++)
            {
                skills[i].IsEmpty = true;
                skills[i].Name = null;
            }
        }

        bool IsSkillOnCooldown(int slot, ClientVersion version, BinaryReader reader, IntPtr processHandle)
        {
            if (version == null || !UpdateSkillbookCooldownPointer(version, reader, processHandle))
                return false;

            if (!IsReadableMemory(processHandle, baseCooldownPointer))
                return false;

            long position = reader.BaseStream.Position;

            try
            {
                var cooldownVariable = version.GetVariable(SkillCooldownsKey) as SearchMemoryVariable;

                if (cooldownVariable == null)
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

        bool UpdateSkillbookCooldownPointer(ClientVersion version, BinaryReader reader, IntPtr processHandle)
        {
            if (version == null)
                return false;

            var position = reader.BaseStream.Position;

            try
            {
                var cooldownVariable = version.GetVariable(SkillCooldownsKey) as SearchMemoryVariable;
                if (cooldownVariable == null)
                    return false;

                if (baseCooldownPointer != IntPtr.Zero)
                    return true;

                var ptrs = scanner.FindAllUInt32((uint)cooldownVariable.Address)
                    .Select(ptr =>
                    {
                        if (cooldownVariable.Offset.IsNegative)
                            ptr = (IntPtr)((uint)ptr - (uint)cooldownVariable.Offset.Offset);
                        else
                            ptr = (IntPtr)((uint)ptr + (uint)cooldownVariable.Offset.Offset);

                        return ptr;
                    })
                    .Where(ptr => IsReadableMemory(processHandle, ptr))
                    .ToList();
                    

                foreach (var ptr in ptrs)
                {
                    if (ptr == IntPtr.Zero)
                        continue;

                    reader.BaseStream.Position = ptr;
                    var cooldownPtr = reader.ReadUInt32();

                    if (cooldownPtr == 0 || !IsReadableMemory(processHandle, cooldownPtr))
                        continue;

                    baseCooldownPointer = (IntPtr)cooldownPtr;
                    Debug.WriteLine($"Found cooldown timers pointer = {cooldownPtr:X}");
                    return true;
                }

                return false;
            }
            catch { baseCooldownPointer = IntPtr.Zero; return false; }
            finally { reader.BaseStream.Position = position; }
        }

        static bool IsReadableMemory(IntPtr processHandle, long address)
        {
            if (address <= 0)
                return false;

            var sizeOfMemoryInfo = Marshal.SizeOf(typeof(MemoryBasicInformation));
            var byteCount = (int)NativeMethods.VirtualQueryEx(processHandle, (IntPtr)address, out var memoryInfo, sizeOfMemoryInfo);

            if (byteCount <= 0)
                return false;

            if (memoryInfo.Type != VirtualMemoryType.Private)
                return false;

            if (memoryInfo.State == VirtualMemoryStatus.Free)
                return false;

            return true;
        }

        #region IEnumerable Methods
        public IEnumerator<Skill> GetEnumerator()
        {
            foreach (var skill in skills)
                if (!skill.IsEmpty)
                    yield return skill;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
