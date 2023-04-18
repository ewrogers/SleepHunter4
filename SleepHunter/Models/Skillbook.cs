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

namespace SleepHunter.Models
{
    public sealed class Skillbook : ObservableObject, IEnumerable<Skill>, IDisposable
    {
        private static readonly string SkillbookKey = @"Skillbook";
        private static readonly string SkillCooldownsKey = "SkillCooldowns";

        public static readonly int TemuairSkillCount = 36;
        public static readonly int MedeniaSkillCount = 36;
        public static readonly int WorldSkillCount = 18;

        private bool isDisposed;
        private Player owner;
        private readonly List<Skill> skills = new List<Skill>(TemuairSkillCount + MedeniaSkillCount + WorldSkillCount);
        private readonly ConcurrentDictionary<string, bool> activeSkills = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private readonly ProcessMemoryScanner scanner;
        private IntPtr baseCooldownPointer;

        public Player Owner
        {
            get => owner;
            set => SetProperty(ref owner, value);
        }

        public int Count => skills.Count((skill) => { return !skill.IsEmpty; });

        public IEnumerable<Skill> Skills => skills;

        public IEnumerable<Skill> TemuairSkills => from skill in skills
                                                   where skill.Panel == InterfacePanel.TemuairSkills && skill.Slot < TemuairSkillCount 
                                                   select skill;

        public IEnumerable<Skill> MedeniaSkills => from skill in skills 
                                                   where skill.Panel == InterfacePanel.MedeniaSkills && skill.Slot < (TemuairSkillCount + MedeniaSkillCount)
                                                   select skill;

        public IEnumerable<Skill> WorldSkills => from skill in skills
                                                 where skill.Panel == InterfacePanel.WorldSkills && skill.Slot < (TemuairSkillCount + MedeniaSkillCount + WorldSkillCount) 
                                                 select skill;

        public IEnumerable<string> ActiveSkills => from skill in activeSkills where skill.Value select skill.Key;

        public Skillbook(Player owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            scanner = new ProcessMemoryScanner(owner.ProcessHandle, leaveOpen: true);

            InitializeSkillbook();
        }

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
                scanner?.Dispose();
            }

            isDisposed = true;
        }

        private void InitializeSkillbook()
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

        public void ClearActiveSkills() => activeSkills.Clear();

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

                            int maximumLevel;
                            if (!Ability.TryParseLevels(name, out name, out var currentLevel, out maximumLevel))
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

                            skills[i].IsOnCooldown = IsSkillOnCooldown(i, version, reader);
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

        private bool IsSkillOnCooldown(int slot, ClientVersion version, BinaryReader reader)
        {
            if (version == null || !UpdateSkillbookCooldownPointer(version, reader))
                return false;

            if (baseCooldownPointer == IntPtr.Zero)
                return false;

            long position = reader.BaseStream.Position;

            try
            {
                if (!(version.GetVariable(SkillCooldownsKey) is SearchMemoryVariable cooldownVariable))
                    return false;

                var offset = cooldownVariable.Offsets.FirstOrDefault();

                if (offset == null)
                    return false;

                var address = (long)baseCooldownPointer + (slot * cooldownVariable.Size);

                reader.BaseStream.Position = address;

                if (address < 0x15000000 || address > 0x30000000)
                    return false;

                address = reader.ReadUInt32();

                if (address < 0x15000000 || address > 0x30000000)
                    return false;

                if (offset.IsNegative)
                    address -= offset.Offset;
                else
                    address += offset.Offset;

                reader.BaseStream.Position = address;
                var isOnCooldown = reader.ReadBoolean();

                return isOnCooldown;
            }
            catch
            {
                baseCooldownPointer = IntPtr.Zero;
                return false;
            }
            finally { reader.BaseStream.Position = position; }
        }

        private bool UpdateSkillbookCooldownPointer(ClientVersion version, BinaryReader reader)
        {
            if (version == null)
                return false;

            var position = reader.BaseStream.Position;

            try
            {
                if (!(version.GetVariable(SkillCooldownsKey) is SearchMemoryVariable cooldownVariable))
                    return false;

                if (baseCooldownPointer != IntPtr.Zero)
                    return true;

                var ptr = scanner.FindUInt32((uint)cooldownVariable.Address);

                if (ptr == IntPtr.Zero)
                    return false;

                if (cooldownVariable.Offset.IsNegative)
                    ptr = (IntPtr)((uint)ptr - (uint)cooldownVariable.Offset.Offset);
                else
                    ptr = (IntPtr)((uint)ptr + (uint)cooldownVariable.Offset.Offset);

                var address = (long)ptr;

                reader.BaseStream.Position = address;
                address = reader.ReadUInt32();

                baseCooldownPointer = (IntPtr)address;
                return true;
            }
            catch { baseCooldownPointer = IntPtr.Zero; return false; }
            finally { reader.BaseStream.Position = position; }
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
