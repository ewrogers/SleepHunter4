using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Metadata;
using SleepHunter.Models;

namespace SleepHunter.Macro
{
    public sealed partial class PlayerMacroConfiguration :
        ObservableObject
    {
        private readonly List<FlowerQueueItem> flowers = new();
        private readonly List<SpellQueueItem> spells = new();

        public PlayerMacroConfiguration(Player client)
        {
            Client = client ??
                throw new ArgumentNullException(nameof(client));
        }

        public event SpellQueueItemEventHandler SpellAdded;

        public event SpellQueueItemEventHandler SpellRemoved;

        public event FlowerQueueItemEventHandler FlowerTargetAdded;

        public event FlowerQueueItemEventHandler FlowerTargetRemoved;

        public Player Client { get; }

        public string Name => Client.Name;

        public IReadOnlyList<SpellQueueItem> QueuedSpells => spells;

        public IReadOnlyList<FlowerQueueItem> FlowerTargets => flowers;

        public int FlowerQueueCount => flowers.Count;

        [ObservableProperty]
        public partial SpellRotationMode SpellQueueRotation { get; set; }

        [ObservableProperty]
        public partial bool UseLyliacVineyard { get; set; }

        [ObservableProperty]
        public partial bool FlowerAlternateCharacters { get; set; }

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
            if (index < 0)
                spells.Add(spell);
            else
                spells.Insert(index, spell);

            SpellAdded?.Invoke(
                this,
                new SpellQueueItemEventArgs(spell));
        }

        public void AddToFlowerQueue(
            FlowerQueueItem flower,
            int index = -1)
        {
            ArgumentNullException.ThrowIfNull(flower);

            if (index < 0)
                flowers.Add(flower);
            else
                flowers.Insert(index, flower);

            FlowerTargetAdded?.Invoke(
                this,
                new FlowerQueueItemEventArgs(flower));
        }

        public bool IsSpellInQueue(string spellName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(spellName);

            return spells.Exists(
                spell => string.Equals(
                    spell.Name,
                    spellName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool RemoveFromSpellQueue(SpellQueueItem spell)
        {
            ArgumentNullException.ThrowIfNull(spell);

            if (!spells.Remove(spell))
                return false;

            SpellRemoved?.Invoke(
                this,
                new SpellQueueItemEventArgs(spell));
            return true;
        }

        public void RemoveFromSpellQueueAtIndex(int index)
        {
            var spell = spells[index];
            spells.RemoveAt(index);
            SpellRemoved?.Invoke(
                this,
                new SpellQueueItemEventArgs(spell));
        }

        public bool RemoveFromFlowerQueue(FlowerQueueItem flower)
        {
            ArgumentNullException.ThrowIfNull(flower);

            if (!flowers.Remove(flower))
                return false;

            FlowerTargetRemoved?.Invoke(
                this,
                new FlowerQueueItemEventArgs(flower));
            return true;
        }

        public void RemoveFromFlowerQueueAtIndex(int index)
        {
            var flower = flowers[index];
            flowers.RemoveAt(index);
            FlowerTargetRemoved?.Invoke(
                this,
                new FlowerQueueItemEventArgs(flower));
        }

        public void ClearSpellQueue()
        {
            var removed = spells.ToArray();
            spells.Clear();

            foreach (var spell in removed)
            {
                SpellRemoved?.Invoke(
                    this,
                    new SpellQueueItemEventArgs(spell));
            }
        }

        public void ClearFlowerQueue()
        {
            var removed = flowers.ToArray();
            flowers.Clear();

            foreach (var flower in removed)
            {
                FlowerTargetRemoved?.Invoke(
                    this,
                    new FlowerQueueItemEventArgs(flower));
            }
        }
    }
}
