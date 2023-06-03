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
        private const string SpellbookKey = @"Spellbook";

        public const int TemuairSpellCount = 36;
        public const int MedeniaSpellCount = 36;
        public const int WorldSpellCount = 18;

        private readonly List<Spell> spells = new(TemuairSpellCount + MedeniaSpellCount + WorldSpellCount);
        private readonly ConcurrentDictionary<string, DateTime> spellCooldownTimestamps = new();

        private string activeSpell;

        public event EventHandler SpellbookUpdated;

        public Player Owner { get; }

        public int Count => spells.Count((spell) => { return !spell.IsEmpty; });

        public IEnumerable<Spell> Spells => 
            from s in spells select s;

        public IEnumerable<Spell> TemuairSpells => 
            from s in spells where s.Panel == InterfacePanel.TemuairSpells && s.Slot < TemuairSpellCount select s;

        public IEnumerable<Spell> MedeniaSpells => 
            from s in spells where s.Panel == InterfacePanel.MedeniaSpells && s.Slot < (TemuairSpellCount + MedeniaSpellCount) select s;

        public IEnumerable<Spell> WorldSpells => 
            from s in spells where s.Panel == InterfacePanel.WorldSpells && s.Slot < (TemuairSpellCount + MedeniaSpellCount + WorldSpellCount) select s;

        public string ActiveSpell
        {
            get => activeSpell;
            set => SetProperty(ref activeSpell, value);
        }

        public Spellbook(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
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
            return spellCooldownTimestamps.TryRemove(spellName, out _);
        }

        public void ClearAllCooldowns() => spellCooldownTimestamps.Clear();

        public void Update()
        {
            Update(Owner.Accessor);
            SpellbookUpdated?.Invoke(this, EventArgs.Empty);
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

            var spellbookVariable = version.GetVariable(SpellbookKey);

            if (spellbookVariable == null)
            {
                ResetDefaults();
                return;
            }

            using var stream = accessor.GetStream();
            using var reader = new BinaryReader(stream, Encoding.ASCII);

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
                    bool hasSpell = reader.ReadInt16() != 0;
                    ushort iconIndex = reader.ReadUInt16();
                    SpellTargetMode targetMode = (SpellTargetMode)reader.ReadByte();
                    string name = reader.ReadFixedString(spellbookVariable.MaxLength);
                    string prompt = reader.ReadFixedString(spellbookVariable.MaxLength);
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

                    spells[i].IsActive = IsActive(spells[i].Name);

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

            Owner.HasFasSpiorad = foundFasSpiorad;
            Owner.HasLyliacPlant = foundLyliacPlant;
            Owner.HasLyliacVineyard = foundLyliacVineyard;
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
