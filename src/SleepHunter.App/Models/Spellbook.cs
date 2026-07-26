using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Models
{
    public sealed class Spellbook : IEnumerable<Spell>
    {
        public const int TemuairSpellCount = 36;
        public const int MedeniaSpellCount = 36;
        public const int WorldSpellCount = 18;

        private readonly Spell[] spells =
            new Spell[
                TemuairSpellCount +
                MedeniaSpellCount +
                WorldSpellCount];

        public IEnumerable<Spell> TemuairSpells =>
            spells.Where(
                spell =>
                    spell.Panel == InterfacePanel.TemuairSpells &&
                    spell.Slot <= TemuairSpellCount);

        public IEnumerable<Spell> MedeniaSpells =>
            spells.Where(
                spell =>
                    spell.Panel == InterfacePanel.MedeniaSpells &&
                    spell.Slot <=
                        TemuairSpellCount + MedeniaSpellCount);

        public IEnumerable<Spell> WorldSpells =>
            spells.Where(
                spell =>
                    spell.Panel == InterfacePanel.WorldSpells &&
                    spell.Slot <= spells.Length);

        public Spellbook()
        {
            for (var index = 0; index < spells.Length; index++)
                spells[index] = Spell.MakeEmpty(index + 1);
        }

        public Spell GetSpell(string spellName)
        {
            if (string.IsNullOrWhiteSpace(spellName))
                return null;

            return spells.FirstOrDefault(
                spell => string.Equals(
                    spell.Name,
                    spellName.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        internal void Apply(SpellbookSnapshot snapshot)
        {
            var observedSpells = snapshot?.Spells ?? [];
            var bySlot = observedSpells.ToDictionary(
                spell => spell.Slot);

            for (var slot = 1; slot <= spells.Length; slot++)
            {
                var spell = spells[slot - 1];
                if (!bySlot.TryGetValue(slot, out var observed))
                {
                    ResetSpell(spell);
                    continue;
                }

                var metadata = SpellMetadataManager.Instance
                    .GetSpell(observed.Name);
                spell.IsEmpty = false;
                spell.Name = observed.Name;
                spell.Icon = GetSpellIcon(observed.Icon);
                spell.TargetType =
                    (AbilityTargetType)observed.ArgumentType;
                spell.CurrentLevel = observed.CurrentLevel;
                spell.MaximumLevel = observed.MaximumLevel;
                spell.NumberOfLines = observed.CastLines;
                spell.ManaCost = observed.ManaCost;
                spell.Cooldown = observed.Cooldown;
                spell.OpensDialog = observed.OpensDialog;
                spell.CanImprove = metadata?.CanImprove ?? true;
                spell.MinHealthPercent =
                    metadata is { MinHealthPercent: > 0 }
                        ? metadata.MinHealthPercent
                        : null;
                spell.MaxHealthPercent =
                    metadata is { MaxHealthPercent: > 0 }
                        ? metadata.MaxHealthPercent
                        : null;
                spell.IsOnCooldown =
                    observed.IsActionDelayed;
                spell.HasClientCooldownProgress = false;
                spell.IsActive = false;
            }
        }

        public void Reset()
        {
            Apply(SpellbookSnapshot.Empty);
        }

        public IEnumerator<Spell> GetEnumerator()
        {
            foreach (var spell in spells)
            {
                if (!spell.IsEmpty)
                    yield return spell;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static void ResetSpell(Spell spell)
        {
            spell.IsEmpty = true;
            spell.Name = null;
            spell.Icon = null;
            spell.TargetType = AbilityTargetType.None;
            spell.CurrentLevel = 0;
            spell.MaximumLevel = 0;
            spell.NumberOfLines = 1;
            spell.ManaCost = 0;
            spell.Cooldown = TimeSpan.Zero;
            spell.OpensDialog = false;
            spell.CanImprove = true;
            spell.MinHealthPercent = null;
            spell.MaxHealthPercent = null;
            spell.IsOnCooldown = false;
            spell.HasClientCooldownProgress = false;
            spell.IsActive = false;
        }

        private static ImageSource GetSpellIcon(int index)
        {
            try
            {
                return IconManager.Instance.GetSpellIcon(index);
            }
            catch
            {
                return null;
            }
        }
    }
}
