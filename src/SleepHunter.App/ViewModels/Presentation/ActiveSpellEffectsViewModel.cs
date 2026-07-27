using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using SleepHunter.Media;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels.Presentation
{
    public sealed class ActiveSpellEffectsViewModel
    {
        private readonly ActiveSpellEffectViewModel[] effects =
            new ActiveSpellEffectViewModel[
                ActiveSpellEffectSnapshot.MaximumSlot];
        private readonly IconManager icons;

        public ActiveSpellEffectsViewModel(
            IconManager icons = null)
        {
            this.icons = icons;

            for (var index = 0; index < effects.Length; index++)
            {
                effects[index] =
                    new ActiveSpellEffectViewModel(index + 1);
            }
        }

        public IReadOnlyList<ActiveSpellEffectViewModel> Effects =>
            effects;

        public bool HasEffects =>
            effects.Any(effect => !effect.IsEmpty);

        internal void Apply(
            ActiveSpellEffectsSnapshot snapshot)
        {
            var observedEffects = snapshot?.Effects ?? [];
            var bySlot = observedEffects.ToDictionary(
                effect => effect.Slot);

            for (var slot = 1; slot <= effects.Length; slot++)
            {
                var effect = effects[slot - 1];
                if (!bySlot.TryGetValue(slot, out var observed))
                {
                    effect.Reset();
                    continue;
                }

                var resolvedIcon =
                    effect.IsEmpty ||
                    effect.IconIndex != observed.Icon
                        ? GetSpellIcon(observed.Icon)
                        : effect.Icon;
                effect.Apply(observed, resolvedIcon);
            }
        }

        internal void Reset() =>
            Apply(ActiveSpellEffectsSnapshot.Empty);

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
