using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation.Skills;

namespace SleepHunter.ViewModels.Editing
{
    public sealed partial class ClientMacroConfiguration :
        ObservableObject
    {
        private readonly ObservableCollection<FlowerQueueItemViewModel> flowers = new();
        private readonly ReadOnlyObservableCollection<FlowerQueueItemViewModel>
            readOnlyFlowers;
        private readonly List<SkillQueueEntry> skills = new();
        private readonly ObservableCollection<SpellQueueItemViewModel> spells = new();
        private readonly ReadOnlyObservableCollection<SpellQueueItemViewModel>
            readOnlySpells;
        private readonly SpellMetadataManager spellMetadata;
        private string name;

        public ClientMacroConfiguration(ClientSession client)
            : this(client, new SpellMetadataManager())
        {
        }

        public ClientMacroConfiguration(
            ClientSession client,
            SpellMetadataManager spellMetadata)
        {
            Client = client ??
                throw new ArgumentNullException(nameof(client));
            this.spellMetadata = spellMetadata ??
                throw new ArgumentNullException(
                    nameof(spellMetadata));
            name = client.Name;
            readOnlyFlowers = new ReadOnlyObservableCollection<
                FlowerQueueItemViewModel>(flowers);
            readOnlySpells = new ReadOnlyObservableCollection<
                SpellQueueItemViewModel>(spells);
            flowers.CollectionChanged +=
                (_, _) => OnPropertyChanged(nameof(FlowerTargets));
            spells.CollectionChanged +=
                (_, _) => OnPropertyChanged(nameof(QueuedSpells));
            PrioritizeAlternateCharacters = true;
            MaximumFlowerXDistance = 10;
            MaximumFlowerYDistance = 10;
        }

        public ClientSession Client { get; }

        public string Name
        {
            get => name;
            private set => SetProperty(ref name, value);
        }

        public ReadOnlyObservableCollection<SpellQueueItemViewModel>
            QueuedSpells => readOnlySpells;

        public ReadOnlyObservableCollection<FlowerQueueItemViewModel>
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

        public List<SpellQueueItemViewModel> GetSpellQueueSnapshot() =>
            [.. spells];

        public List<FlowerQueueItemViewModel> GetFlowerQueueSnapshot() =>
            [.. flowers];

        internal void UpdateCharacterName(string characterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(characterName);
            Name = characterName.Trim();
            Client.Name = Name;
        }

        public void AddToSpellQueue(
            SpellQueueItemViewModel spell,
            int index = -1)
        {
            ArgumentNullException.ThrowIfNull(spell);

            spell.IsUndefined =
                !spellMetadata.ContainsSpell(spell.Name);
            EnsureSpellIdentifier(spell);
            if (index < 0)
                spells.Add(spell);
            else
                spells.Insert(index, spell);
        }

        public void AddToFlowerQueue(
            FlowerQueueItemViewModel flower,
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

            OnPropertyChanged(nameof(Skills));

            return isActive;
        }

        public void ClearSkills()
        {
            skills.Clear();
            OnPropertyChanged(nameof(Skills));
        }

        public bool RemoveFromSpellQueue(SpellQueueItemViewModel spell)
        {
            ArgumentNullException.ThrowIfNull(spell);

            return spells.Remove(spell);
        }

        public bool RemoveFromFlowerQueue(FlowerQueueItemViewModel flower)
        {
            ArgumentNullException.ThrowIfNull(flower);

            return flowers.Remove(flower);
        }

        public bool UpdateSpell(
            SpellQueueItemViewModel spell,
            SpellQueueItemViewModel replacement)
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
            FlowerQueueItemViewModel flower,
            FlowerQueueItemViewModel replacement)
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
            SpellQueueItemViewModel spell,
            SpellQueueItemViewModel target)
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
            FlowerQueueItemViewModel flower,
            FlowerQueueItemViewModel target)
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

        private void EnsureSpellIdentifier(SpellQueueItemViewModel spell)
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

        private void EnsureFlowerIdentifier(FlowerQueueItemViewModel flower)
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
