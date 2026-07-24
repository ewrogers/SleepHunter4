using System;
using System.Buffers.Binary;
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
    public sealed class Spellbook : UpdatableObject, IEnumerable<Spell>
    {
        private const string SpellbookKey = @"Spellbook";
        private const string SpellbookPanesKey = @"SpellbookPanes";
        private const string SpellbookPaneCapacityKey = @"SpellbookPaneCapacity";
        private const int SpellPaneSnapshotSize = 0x12C;

        public const int TemuairSpellCount = 36;
        public const int MedeniaSpellCount = 36;
        public const int WorldSpellCount = 18;

        private readonly Spell[] spells = new Spell[TemuairSpellCount + MedeniaSpellCount + WorldSpellCount];

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private readonly ConcurrentDictionary<string, DateTime> spellCooldownTimestamps = new();

        private string activeSpell;

        public Player Owner { get; init; }

        public IEnumerable<Spell> AllSpells => 
            from s in spells select s;

        public IEnumerable<Spell> TemuairSpells => 
            from s in spells where s.Panel == InterfacePanel.TemuairSpells && s.Slot <= TemuairSpellCount select s;

        public IEnumerable<Spell> MedeniaSpells => 
            from s in spells where s.Panel == InterfacePanel.MedeniaSpells && s.Slot <= (TemuairSpellCount + MedeniaSpellCount) select s;

        public IEnumerable<Spell> WorldSpells => 
            from s in spells where s.Panel == InterfacePanel.WorldSpells && s.Slot <= (TemuairSpellCount + MedeniaSpellCount + WorldSpellCount) select s;

        public string ActiveSpell
        {
            get => activeSpell;
            set => SetProperty(ref activeSpell, value);
        }

        public Spellbook(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);

            for (var i = 0; i < spells.Length; i++)
                spells[i] = (Spell.MakeEmpty(i + 1));
        }

        public bool ContainSpell(string spellName)
        {
            CheckIfDisposed();
            return spells.Any(spell => string.Equals(spell.Name, spellName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public Spell GetSpell(string spellName)
        {
            CheckIfDisposed();
            return spells.FirstOrDefault(spell => string.Equals(spell.Name, spellName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public bool IsActive(string spellName)
        {
            CheckIfDisposed();

            if (spellName == null)
                return false;

            return string.Equals(activeSpell, spellName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public void SetCooldownTimestamp(string spellName, DateTime timestamp)
        {
            CheckIfDisposed();

            spellName = spellName.Trim();
            spellCooldownTimestamps[spellName] = timestamp;
        }

        public bool ClearCooldown(string spellName)
        {
            CheckIfDisposed();

            spellName = spellName.Trim();
            return spellCooldownTimestamps.TryRemove(spellName, out _);
        }

        public void ClearAllCooldowns()
        {
            CheckIfDisposed();
            spellCooldownTimestamps.Clear();
        }

        protected override void OnUpdate()
        {
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                UpdateCooldowns();
                return;
            }

            if (TryUpdateFromPanes(version))
            {
                UpdateCooldowns();
                return;
            }

            if (!version.TryGetVariable(SpellbookKey, out var spellbookVariable))
            {
                ResetDefaults();
                UpdateCooldowns();
                return;
            }

            if (!spellbookVariable.TryDereferenceValue(reader, out var basePointer))
            {
                ResetDefaults();
                UpdateCooldowns();
                return;
            }

            stream.Position = basePointer;

            var foundFasSpiorad = false;
            var foundLyliacVineyard = false;
            var foundLyliacPlant = false;

            var entryCount = Math.Min(spells.Length, spellbookVariable.Count);

            for (var i = 0; i < entryCount; i++)
            {
                SpellMetadata metadata = null;

                try
                {
                    var hasSpell = reader.ReadInt16() != 0;
                    var iconIndex = reader.ReadUInt16();
                    var targetType = (AbilityTargetType)reader.ReadByte();
                    var rawName = reader.ReadFixedString(spellbookVariable.MaxLength);
                    var name = rawName;
                    var prompt = reader.ReadFixedString(spellbookVariable.MaxLength);
                    reader.ReadByte();

                    if (!Ability.TryParseLevels(rawName, out name, out var currentLevel, out var maximumLevel))
                    {
                        name = rawName.Trim();
                        currentLevel = 0;
                        maximumLevel = 0;
                    }

                    spells[i].IsEmpty = !hasSpell;
                    spells[i].IconIndex = iconIndex;
                    spells[i].Icon = IconManager.Instance.GetSpellIcon(iconIndex);
                    spells[i].TargetType = targetType;
                    spells[i].Name = name;
                    spells[i].Prompt = prompt;
                    spells[i].CurrentLevel = currentLevel;
                    spells[i].MaximumLevel = maximumLevel;
                    ResetClientPaneState(spells[i]);

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

            for (var i = entryCount; i < spells.Length; i++)
                ResetSpell(spells[i]);

            Owner.HasFasSpiorad = foundFasSpiorad;
            Owner.HasLyliacPlant = foundLyliacPlant;
            Owner.HasLyliacVineyard = foundLyliacVineyard;

            UpdateCooldowns();
        }

        private bool TryUpdateFromPanes(Settings.ClientVersion version)
        {
            if (!version.TryGetVariable(SpellbookPanesKey, out var panesVariable) ||
                !version.TryGetVariable(SpellbookPaneCapacityKey, out var capacityVariable) ||
                !capacityVariable.TryReadInt32(reader, out var capacity) ||
                capacity <= 0 ||
                capacity > spells.Length ||
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

            var records = new SpellPaneRecord?[spells.Length];
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
                    SpellPaneSnapshotSize,
                    out var snapshot))
                {
                    return false;
                }

                var record = ParseSpellPaneSnapshot(snapshot);
                if (record.Slot == 0 || record.Slot > spells.Length || records[record.Slot - 1].HasValue)
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

            for (var index = 0; index < spells.Length; index++)
            {
                if (!records[index].HasValue)
                {
                    ResetSpell(spells[index]);
                    continue;
                }

                var record = records[index].Value;
                var spell = spells[index];
                ParsePaneAbilityName(
                    record.Name,
                    record.NameSuffixLeft,
                    record.BaseNameLength,
                    out var name,
                    out var currentLevel,
                    out var maximumLevel);

                spell.IsEmpty = string.IsNullOrWhiteSpace(name);
                spell.IconIndex = record.IconIndex;
                spell.Icon = spell.IsEmpty ? null : IconManager.Instance.GetSpellIcon(record.IconIndex);
                spell.TargetType = record.TargetType;
                spell.Name = name;
                spell.Prompt = record.Prompt;
                spell.CurrentLevel = currentLevel;
                spell.MaximumLevel = maximumLevel;
                spell.IsActive = IsActive(spell.Name);
                spell.IsActionDelayed = record.ActionDelayActive;
                spell.ClientNameSuffixLeft = record.NameSuffixLeft;
                spell.ClientNameSuffixRight = record.NameSuffixRight;
                spell.ClientBaseNameLength = record.BaseNameLength;
                ApplyMetadata(spell, record.CastLines);
            }

            UpdateSpecialSpellFlags();
            return true;
        }

        internal static SpellPaneRecord ParseSpellPaneSnapshot(ReadOnlySpan<byte> snapshot)
        {
            if (snapshot.Length != SpellPaneSnapshotSize)
                throw new InvalidDataException(
                    $"A spell pane snapshot must contain {SpellPaneSnapshotSize} bytes.");

            return new SpellPaneRecord(
                snapshot[0x00],
                BinaryPrimitives.ReadUInt16LittleEndian(snapshot.Slice(0x02, 2)),
                (AbilityTargetType)snapshot[0x04],
                ReadNullTerminatedAscii(snapshot.Slice(0x05, 0x80)),
                ReadNullTerminatedAscii(snapshot.Slice(0x85, 0x80)),
                snapshot[0x105],
                snapshot[0x107] != 0,
                BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x120, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x124, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x128, 4)));
        }

        internal readonly record struct SpellPaneRecord(
            byte Slot,
            ushort IconIndex,
            AbilityTargetType TargetType,
            string Name,
            string Prompt,
            byte CastLines,
            bool ActionDelayActive,
            int NameSuffixLeft,
            int NameSuffixRight,
            int BaseNameLength);

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

        private static void ApplyMetadata(Spell spell, byte clientCastLines)
        {
            var metadata = !spell.IsEmpty && !string.IsNullOrWhiteSpace(spell.Name)
                ? SpellMetadataManager.Instance.GetSpell(spell.Name)
                : null;

            spell.NumberOfLines = clientCastLines > 0
                ? clientCastLines
                : metadata?.NumberOfLines ?? 1;

            if (metadata != null)
            {
                spell.ManaCost = metadata.ManaCost;
                spell.Cooldown = metadata.Cooldown;
                spell.OpensDialog = metadata.OpensDialog;
                spell.CanImprove = metadata.CanImprove;
                spell.MinHealthPercent = metadata.MinHealthPercent > 0 ? metadata.MinHealthPercent : null;
                spell.MaxHealthPercent = metadata.MaxHealthPercent > 0 ? metadata.MaxHealthPercent : null;
            }
            else
            {
                spell.ManaCost = 0;
                spell.Cooldown = TimeSpan.Zero;
                spell.OpensDialog = false;
                spell.CanImprove = true;
                spell.MinHealthPercent = null;
                spell.MaxHealthPercent = null;
            }
        }

        private void UpdateSpecialSpellFlags()
        {
            Owner.HasFasSpiorad = spells.Any(spell =>
                !spell.IsEmpty &&
                string.Equals(spell.Name, Spell.FasSpioradKey, StringComparison.OrdinalIgnoreCase));
            Owner.HasLyliacPlant = spells.Any(spell =>
                !spell.IsEmpty &&
                string.Equals(spell.Name, Spell.LyliacPlantKey, StringComparison.OrdinalIgnoreCase));
            Owner.HasLyliacVineyard = spells.Any(spell =>
                !spell.IsEmpty &&
                string.Equals(spell.Name, Spell.LyliacVineyardKey, StringComparison.OrdinalIgnoreCase));
        }

        private static void ResetClientPaneState(Spell spell)
        {
            spell.IsActionDelayed = false;
            spell.ClientNameSuffixLeft = 0;
            spell.ClientNameSuffixRight = 0;
            spell.ClientBaseNameLength = 0;
        }

        private static void ResetSpell(Spell spell)
        {
            spell.IsEmpty = true;
            spell.IconIndex = 0;
            spell.Icon = null;
            spell.Name = null;
            spell.Prompt = null;
            spell.CurrentLevel = 0;
            spell.MaximumLevel = 0;
            spell.IsActive = false;
            spell.IsOnCooldown = false;
            spell.NumberOfLines = 1;
            spell.ManaCost = 0;
            spell.Cooldown = TimeSpan.Zero;
            spell.OpensDialog = false;
            spell.CanImprove = true;
            spell.MinHealthPercent = null;
            spell.MaxHealthPercent = null;
            ResetClientPaneState(spell);
        }

        public void ResetDefaults()
        {
            ActiveSpell = null;

            for (int i = 0; i < spells.Length; i++)
                ResetSpell(spells[i]);

            Owner.HasFasSpiorad = false;
            Owner.HasLyliacPlant = false;
            Owner.HasLyliacVineyard = false;
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
            for (var i = 0; i < spells.Length; i++)
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
