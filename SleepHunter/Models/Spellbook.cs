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
                UpdateCooldowns();
                return;
            }

            var spellbookVariable = version.GetVariable(SpellbookKey);

            if (spellbookVariable == null)
            {
                ResetDefaults();
                UpdateCooldowns();
                return;
            }

            using var stream = accessor.GetStream();
            using var reader = new BinaryReader(stream, Encoding.ASCII);

            var spellbookPointer = spellbookVariable.DereferenceValue(reader);

            if (spellbookPointer == 0)
            {
                ResetDefaults();
                UpdateCooldowns();
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
                    var targetType = (AbilityTargetType)reader.ReadByte();
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
                    spells[i].TargetType = targetType;
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
                        spells[i].OpensDialog = metadata.OpensDialog;
                        spells[i].CanImprove = metadata.CanImprove;
                        spells[i].MinHealthPercent = metadata.MinHealthPercent > 0 ? metadata.MinHealthPercent : null;
                        spells[i].MaxHealthPercent = metadata.MaxHealthPercent > 0 ? metadata.MaxHealthPercent : null;
                    }
                    else
                    {
                        spells[i].NumberOfLines = 1;
                        spells[i].ManaCost = 0;
                        spells[i].Cooldown = TimeSpan.Zero;
                        spells[i].OpensDialog = false;
                        spells[i].CanImprove = true;
                        spells[i].MinHealthPercent = null;
                        spells[i].MaxHealthPercent = null;
                    }
                }
                catch { }
            }

            Owner.HasFasSpiorad = foundFasSpiorad;
            Owner.HasLyliacPlant = foundLyliacPlant;
            Owner.HasLyliacVineyard = foundLyliacVineyard;

            UpdateCooldowns();
        }

        public void ResetDefaults()
        {
            ActiveSpell = null;

            for (int i = 0; i < spells.Capacity; i++)
            {
                spells[i].IsEmpty = true;
                spells[i].Name = null;
                spells[i].IsOnCooldown = false;
            }
        }

        public IEnumerator<Spell> GetEnumerator()
        {
            foreach (var spell in spells)
                if (!spell.IsEmpty)
                    yield return spell;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void UpdateCooldowns()
        {
            for (var i = 0; i < spells.Capacity; i++)
            {
                var spellName = spells[i].Name;

                if (spells[i].IsEmpty || string.IsNullOrWhiteSpace(spellName))
                {
                    spells[i].IsOnCooldown = false;
                    continue;
                }

                if (!spellCooldownTimestamps.TryGetValue(spells[i].Name, out var lastUsedTimestamp))
                {
                    spells[i].IsOnCooldown = false;
                    continue;
                }

                var cooldownTicks = spells[i].Cooldown.TotalSeconds * TimeSpan.TicksPerSecond;
                var readyAtTicks = lastUsedTimestamp.Ticks + cooldownTicks;

                spells[i].IsOnCooldown = readyAtTicks > DateTime.Now.Ticks;
            }
        }
    }
}
