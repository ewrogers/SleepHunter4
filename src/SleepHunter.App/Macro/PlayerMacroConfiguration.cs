using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation.Skills;

namespace SleepHunter.Macro
{
    public sealed partial class PlayerMacroConfiguration :
        ObservableObject
    {
        private readonly ObservableCollection<FlowerQueueItem> flowers = new();
        private readonly ReadOnlyObservableCollection<FlowerQueueItem>
            readOnlyFlowers;
        private readonly List<SkillQueueEntry> skills = new();
        private readonly ObservableCollection<SpellQueueItem> spells = new();
        private readonly ReadOnlyObservableCollection<SpellQueueItem>
            readOnlySpells;

        public PlayerMacroConfiguration(Player client)
        {
            Client = client ??
                throw new ArgumentNullException(nameof(client));
            readOnlyFlowers = new ReadOnlyObservableCollection<
                FlowerQueueItem>(flowers);
            readOnlySpells = new ReadOnlyObservableCollection<
                SpellQueueItem>(spells);
            flowers.CollectionChanged +=
                (_, _) => OnPropertyChanged(nameof(FlowerTargets));
            spells.CollectionChanged +=
                (_, _) => OnPropertyChanged(nameof(QueuedSpells));
            PrioritizeAlternateCharacters = true;
            MaximumFlowerXDistance = 10;
            MaximumFlowerYDistance = 10;
        }

        public Player Client { get; }

        public string Name => Client.Name;

        public ReadOnlyObservableCollection<SpellQueueItem>
            QueuedSpells => readOnlySpells;

        public ReadOnlyObservableCollection<FlowerQueueItem>
            FlowerTargets => readOnlyFlowers;

        public IReadOnlyList<SkillQueueEntry> Skills => skills;

        [ObservableProperty]
        public partial string Description { get; set; }

        [ObservableProperty]
        public partial SpellRotationMode SpellQueueRotation { get; set; }

        [ObservableProperty]
        public partial bool UseLyliacVineyard { get; set; }

        [ObservableProperty]
        public partial bool FlowerAlternateCharacters { get; set; }

        [ObservableProperty]
        public partial bool PrioritizeAlternateCharacters { get; set; }

        [ObservableProperty]
        public partial int MaximumFlowerXDistance { get; set; }

        [ObservableProperty]
        public partial int MaximumFlowerYDistance { get; set; }

        public List<SkillQueueEntry> GetSkillQueueSnapshot() =>
            [.. skills];

        public List<SpellQueueItem> GetSpellQueueSnapshot() =>
            [.. spells];

        public List<FlowerQueueItem> GetFlowerQueueSnapshot() =>
            [.. flowers];

        public void AddToSpellQueue(
            SpellQueueItem spell,
            int index = -1)
        {
            ArgumentNullException.ThrowIfNull(spell);

            spell.IsUndefined =
                !SpellMetadataManager.Instance.ContainsSpell(spell.Name);
            EnsureSpellIdentifier(spell);
            if (index < 0)
                spells.Add(spell);
            else
                spells.Insert(index, spell);
        }

        public void AddToFlowerQueue(
            FlowerQueueItem flower,
            int index = -1)
        {
            ArgumentNullException.ThrowIfNull(flower);

            EnsureFlowerIdentifier(flower);
            if (index < 0)
                flowers.Add(flower);
            else
                flowers.Insert(index, flower);
        }

        public bool IsSpellInQueue(string spellName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(spellName);

            return spells.Any(
                spell => string.Equals(
                    spell.Name,
                    spellName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        public void ReplaceSkills(
            IEnumerable<SkillQueueEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var replacement = entries.ToList();
            if (replacement.Any(entry => entry is null))
            {
                throw new ArgumentException(
                    "Skill entries cannot contain null values.",
                    nameof(entries));
            }

            skills.Clear();
            skills.AddRange(replacement);
            Client.Skillbook.ClearActiveSkills();
            foreach (var entry in skills)
            {
                Client.Skillbook.ToggleActive(
                    entry.Name,
                    isActive: true);
            }

            OnPropertyChanged(nameof(Skills));
        }

        public bool ToggleSkill(string skillName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(skillName);

            var normalized = skillName.Trim();
            var index = skills.FindIndex(
                entry => string.Equals(
                    entry.Name,
                    normalized,
                    StringComparison.OrdinalIgnoreCase));
            var isActive = index < 0;
            if (isActive)
            {
                var identifier = skills.Count == 0
                    ? 1
                    : skills.Max(entry => entry.Id.Value) + 1;
                skills.Add(
                    new SkillQueueEntry(
                        new SkillQueueEntryId(identifier),
                        normalized));
            }
            else
            {
                skills.RemoveAt(index);
            }

            Client.Skillbook.ToggleActive(
                normalized,
                isActive);
            OnPropertyChanged(nameof(Skills));

            return isActive;
        }

        public void ClearSkills()
        {
            skills.Clear();
            Client.Skillbook.ClearActiveSkills();
            OnPropertyChanged(nameof(Skills));
        }

        public bool RemoveFromSpellQueue(SpellQueueItem spell)
        {
            ArgumentNullException.ThrowIfNull(spell);

            return spells.Remove(spell);
        }

        public bool RemoveFromFlowerQueue(FlowerQueueItem flower)
        {
            ArgumentNullException.ThrowIfNull(flower);

            return flowers.Remove(flower);
        }

        public bool UpdateSpell(
            SpellQueueItem spell,
            SpellQueueItem replacement)
        {
            ArgumentNullException.ThrowIfNull(spell);
            ArgumentNullException.ThrowIfNull(replacement);

            if (!spells.Contains(spell))
                return false;

            replacement.CopyTo(spell);
            OnPropertyChanged(nameof(QueuedSpells));
            return true;
        }

        public bool UpdateFlower(
            FlowerQueueItem flower,
            FlowerQueueItem replacement)
        {
            ArgumentNullException.ThrowIfNull(flower);
            ArgumentNullException.ThrowIfNull(replacement);

            if (!flowers.Contains(flower))
                return false;

            replacement.CopyTo(flower);
            OnPropertyChanged(nameof(FlowerTargets));
            return true;
        }

        public bool MoveSpell(
            SpellQueueItem spell,
            SpellQueueItem target)
        {
            ArgumentNullException.ThrowIfNull(spell);
            ArgumentNullException.ThrowIfNull(target);

            var oldIndex = spells.IndexOf(spell);
            var newIndex = spells.IndexOf(target);
            if (oldIndex < 0 ||
                newIndex < 0 ||
                oldIndex == newIndex)
            {
                return false;
            }

            spells.Move(oldIndex, newIndex);
            return true;
        }

        public bool MoveFlower(
            FlowerQueueItem flower,
            FlowerQueueItem target)
        {
            ArgumentNullException.ThrowIfNull(flower);
            ArgumentNullException.ThrowIfNull(target);

            var oldIndex = flowers.IndexOf(flower);
            var newIndex = flowers.IndexOf(target);
            if (oldIndex < 0 ||
                newIndex < 0 ||
                oldIndex == newIndex)
            {
                return false;
            }

            flowers.Move(oldIndex, newIndex);
            return true;
        }

        public void ClearSpellQueue() => spells.Clear();

        public void ClearFlowerQueue() => flowers.Clear();

        private void EnsureSpellIdentifier(SpellQueueItem spell)
        {
            if (spell.Id > 0 &&
                !spells.Any(
                    existing =>
                        !ReferenceEquals(existing, spell) &&
                        existing.Id == spell.Id))
            {
                return;
            }

            spell.Id = spells.Count == 0
                ? 1
                : spells.Max(existing => existing.Id) + 1;
        }

        private void EnsureFlowerIdentifier(FlowerQueueItem flower)
        {
            if (flower.Id > 0 &&
                !flowers.Any(
                    existing =>
                        !ReferenceEquals(existing, flower) &&
                        existing.Id == flower.Id))
            {
                return;
            }

            flower.Id = flowers.Count == 0
                ? 1
                : flowers.Max(existing => existing.Id) + 1;
        }
    }
}
