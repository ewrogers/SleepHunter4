using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.Common;
using SleepHunter.Extensions;
using SleepHunter.IO.Process;
using SleepHunter.Media;
using SleepHunter.Metadata;

namespace SleepHunter.Models
{
    public sealed class Spellbook : ObservableObject, IEnumerable<Spell>
    {
        private static readonly string SpellbookKey = @"Spellbook";

        public static readonly int TemuairSpellCount = 36;
        public static readonly int MedeniaSpellCount = 36;
        public static readonly int WorldSpellCount = 18;

        private Player owner;
        private readonly List<Spell> spells = new List<Spell>(TemuairSpellCount + MedeniaSpellCount + WorldSpellCount);
        private readonly ConcurrentDictionary<string, DateTime> spellCooldownTimestamps = new ConcurrentDictionary<string, DateTime>();
        private string activeSpell;

        public Player Owner
        {
            get => owner;
            set => SetProperty(ref owner, value);
        }

        public int Count => spells.Count((spell) => { return !spell.IsEmpty; });

        public IEnumerable<Spell> Spells => spells;

        public IEnumerable<Spell> TemuairSpells => from spell in spells
                                                   where spell.Panel == InterfacePanel.TemuairSpells && spell.Slot < TemuairSpellCount
                                                   select spell;

        public IEnumerable<Spell> MedeniaSpells => from spell in spells
                                                   where spell.Panel == InterfacePanel.MedeniaSpells && spell.Slot < (TemuairSpellCount + MedeniaSpellCount)
                                                   select spell;

        public IEnumerable<Spell> WorldSpells => from spell in spells
                                                 where spell.Panel == InterfacePanel.WorldSpells && spell.Slot < (TemuairSpellCount + MedeniaSpellCount + WorldSpellCount)
                                                 select spell;

        public string ActiveSpell
        {
            get => activeSpell;
            set => SetProperty(ref activeSpell, value);
        }

        public Spellbook(Player owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            InitialzeSpellbook();
        }

        private void InitialzeSpellbook()
        {
            spells.Clear();

            for (int i = 0; i < spells.Capacity; i++)
                spells.Add(Spell.MakeEmpty(i + 1));
        }

        public bool ContainSpell(string spellName)
        {
            spellName = spellName.Trim();

            foreach (var spell in spells)
                if (string.Equals(spell.Name, spellName, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public Spell GetSpell(string spellName)
        {
            spellName = spellName.Trim();

            foreach (var spell in spells)
                if (string.Equals(spell.Name, spellName, StringComparison.OrdinalIgnoreCase))
                    return spell;

            return null;
        }

        public bool IsActive(string spellName)
        {
            if (spellName == null)
                return false;

            spellName = spellName.Trim();

            return string.Equals(spellName, activeSpell, StringComparison.OrdinalIgnoreCase);
        }

        public void SetCooldownTimestamp(string spellName, DateTime timestamp)
        {
            spellName = spellName.Trim();
            spellCooldownTimestamps[spellName] = timestamp;
        }

        public bool ClearCooldown(string spellName)
        {
            spellName = spellName.Trim();
            return spellCooldownTimestamps.TryRemove(spellName, out var _);
        }

        public void ClearAllCooldowns() => spellCooldownTimestamps.Clear();

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

            var spellbookVariable = version.GetVariable(SpellbookKey);

            if (spellbookVariable == null)
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
                    var spellbookPointer = spellbookVariable.DereferenceValue(reader);

                    if (spellbookPointer == 0)
                    {
                        ResetDefaults();
                        return;
                    }

                    reader.BaseStream.Position = spellbookPointer;
                    bool foundFasSpiorad = false;
                    bool foundLyliacVineyard = false;
                    bool foundLyliacPlant = false;

                    for (int i = 0; i < spellbookVariable.Count; i++)
                    {
                        SpellMetadata metadata = null;

                        try
                        {
                            var hasSpell = reader.ReadInt16() != 0;
                            var iconIndex = reader.ReadUInt16();
                            var targetMode = (SpellTargetMode)reader.ReadByte();
                            var name = reader.ReadFixedString(spellbookVariable.MaxLength);
                            var prompt = reader.ReadFixedString(spellbookVariable.MaxLength);
                            reader.ReadByte();

                            if (!Ability.TryParseLevels(name, out name, out var currentLevel, out var maximumLevel))
                            {
                                if (!string.IsNullOrWhiteSpace(name))
                                    spells[i].Name = name.Trim();
                            }


                            spells[i].IsEmpty = !hasSpell;
                            spells[i].IconIndex = iconIndex;
                            spells[i].Icon = IconManager.Instance.GetSpellIcon(iconIndex);
                            spells[i].TargetMode = targetMode;
                            spells[i].Name = name;
                            spells[i].Prompt = prompt;
                            spells[i].CurrentLevel = currentLevel;
                            spells[i].MaximumLevel = maximumLevel;

                            if (!spells[i].IsEmpty && !string.IsNullOrWhiteSpace(spells[i].Name))
                                metadata = SpellMetadataManager.Instance.GetSpell(name);

                            spells[i].IsActive = this.IsActive(spells[i].Name);

                            foundFasSpiorad |= string.Equals(spells[i].Name, Spell.FasSpioradKey, StringComparison.OrdinalIgnoreCase);
                            foundLyliacPlant |= string.Equals(spells[i].Name, Spell.LyliacPlantKey, StringComparison.OrdinalIgnoreCase);
                            foundLyliacVineyard |= string.Equals(spells[i].Name, Spell.LyliacVineyardKey, StringComparison.OrdinalIgnoreCase);

                            if (metadata != null)
                            {
                                spells[i].NumberOfLines = metadata.NumberOfLines;
                                spells[i].ManaCost = metadata.ManaCost;
                                spells[i].Cooldown = metadata.Cooldown;
                                spells[i].CanImprove = metadata.CanImprove;

                                DateTime timestamp = DateTime.MinValue;
                                if (spells[i].Cooldown > TimeSpan.Zero && spellCooldownTimestamps.TryGetValue(name, out timestamp))
                                {
                                    var elapsed = DateTime.Now - timestamp;
                                    spells[i].IsOnCooldown = elapsed < (spells[i].Cooldown + TimeSpan.FromMilliseconds(500));
                                }
                            }
                            else
                            {
                                spells[i].NumberOfLines = 1;
                                spells[i].ManaCost = 0;
                                spells[i].Cooldown = TimeSpan.Zero;
                                spells[i].CanImprove = true;
                                spells[i].IsOnCooldown = false;
                            }
                        }
                        catch { }
                    }

                    owner.HasFasSpiorad = foundFasSpiorad;
                    owner.HasLyliacPlant = foundLyliacPlant;
                    owner.HasLyliacVineyard = foundLyliacVineyard;
                }
            }
            finally { stream?.Dispose(); }
        }

        public void ResetDefaults()
        {
            ActiveSpell = null;

            for (int i = 0; i < spells.Capacity; i++)
            {
                spells[i].IsEmpty = true;
                spells[i].Name = null;
            }
        }

        public IEnumerator<Spell> GetEnumerator()
        {
            foreach (var spell in spells)
                if (!spell.IsEmpty)
                    yield return spell;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
