using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels.Presentation
{
    public sealed class SpellbookViewModel :
        IEnumerable<SpellViewModel>
    {
        public const int TemuairSpellCount = 36;
        public const int MedeniaSpellCount = 36;
        public const int WorldSpellCount = 18;

        private readonly SpellViewModel[] spells =
            new SpellViewModel[
                TemuairSpellCount +
                MedeniaSpellCount +
                WorldSpellCount];
        private readonly IconManager icons;
        private readonly SpellMetadataManager metadata;

        public IEnumerable<SpellViewModel> TemuairSpells =>
            spells.Where(
                spell =>
                    spell.Panel == ClientPanel.TemuairSpells &&
                    spell.Slot <= TemuairSpellCount);

        public IEnumerable<SpellViewModel> MedeniaSpells =>
            spells.Where(
                spell =>
                    spell.Panel == ClientPanel.MedeniaSpells &&
                    spell.Slot <=
                        TemuairSpellCount + MedeniaSpellCount);

        public IEnumerable<SpellViewModel> WorldSpells =>
            spells.Where(
                spell =>
                    spell.Panel == ClientPanel.WorldSpells &&
                    spell.Slot <= spells.Length);

        public SpellbookViewModel(
            IconManager icons = null,
            SpellMetadataManager metadata = null)
        {
            this.icons = icons;
            this.metadata = metadata ??
                new SpellMetadataManager();

            for (var index = 0; index < spells.Length; index++)
                spells[index] =
                    SpellViewModel.MakeEmpty(index + 1);
        }

        public SpellViewModel GetSpell(string spellName)
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

                var spellMetadata =
                    metadata.GetSpell(observed.Name);
                spell.IsEmpty = false;
                spell.Name = observed.Name;
                spell.Icon = GetSpellIcon(observed.Icon);
                spell.ArgumentType = observed.ArgumentType;
                spell.CurrentLevel = observed.CurrentLevel;
                spell.MaximumLevel = observed.MaximumLevel;
                spell.NumberOfLines = observed.CastLines;
                spell.ManaCost = observed.ManaCost;
                spell.Cooldown = observed.Cooldown;
                spell.OpensDialog = observed.OpensDialog;
                spell.CanImprove =
                    spellMetadata?.CanImprove ?? true;
                spell.MinHealthPercent =
                    spellMetadata is { MinHealthPercent: > 0 }
                        ? spellMetadata.MinHealthPercent
                        : null;
                spell.MaxHealthPercent =
                    spellMetadata is { MaxHealthPercent: > 0 }
                        ? spellMetadata.MaxHealthPercent
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

        public IEnumerator<SpellViewModel> GetEnumerator()
        {
            foreach (var spell in spells)
            {
                if (!spell.IsEmpty)
                    yield return spell;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static void ResetSpell(
            SpellViewModel spell)
        {
            spell.IsEmpty = true;
            spell.Name = null;
            spell.Icon = null;
            spell.ArgumentType = SpellArgumentType.None;
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

        private ImageSource GetSpellIcon(int index)
        {
            try
            {
                return icons?.GetSpellIcon(index);
            }
            catch
            {
                return null;
            }
        }
    }
}
