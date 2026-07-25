using System;
using System.Collections.Generic;
using System.Linq;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Metadata;
using SleepHunter.Runtime.Automation;

namespace SleepHunter.Services.Runtime
{
    public static class AbilitySnapshotCatalogFactory
    {
        public static AbilitySnapshotCatalog Create(
            IEnumerable<SkillMetadata> skills,
            IEnumerable<SpellMetadata> spells)
        {
            ArgumentNullException.ThrowIfNull(skills);
            ArgumentNullException.ThrowIfNull(spells);

            return new AbilitySnapshotCatalog(
                skills.Select(
                    skill => new SkillSnapshotMetadata(
                        skill.Name,
                        skill.ManaCost,
                        skill.Cooldown,
                        skill.IsAssail,
                        skill.OpensDialog,
                        skill.RequiresDisarm,
                        CreateHealthCondition(
                            skill.MinHealthPercent,
                            skill.MaxHealthPercent))),
                spells.Select(
                    spell => new SpellSnapshotMetadata(
                        spell.Name,
                        spell.NumberOfLines,
                        spell.ManaCost,
                        spell.Cooldown,
                        spell.OpensDialog)));
        }

        private static HealthCondition CreateHealthCondition(
            double minimumPercent,
            double maximumPercent) =>
            new(
                minimumPercent > 0
                    ? minimumPercent
                    : null,
                maximumPercent > 0
                    ? maximumPercent
                    : null);
    }
}
