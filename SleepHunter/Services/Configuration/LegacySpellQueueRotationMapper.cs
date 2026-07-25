using System;
using SleepHunter.Macro;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Services.Configuration
{
    public static class LegacySpellQueueRotationMapper
    {
        public static SpellQueueRotation Map(
            SpellRotationMode rotation) =>
            rotation switch
            {
                SpellRotationMode.Default => SpellQueueRotation.Priority,
                SpellRotationMode.None => SpellQueueRotation.Priority,
                SpellRotationMode.Singular => SpellQueueRotation.Sequential,
                SpellRotationMode.RoundRobin => SpellQueueRotation.RoundRobin,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(rotation),
                    rotation,
                    "The legacy spell rotation is not supported.")
            };
    }
}
