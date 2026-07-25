using SleepHunter.Metadata;
using SleepHunter.Services.Runtime;

namespace SleepHunter.Tests.Services.Runtime;

public sealed class AbilitySnapshotCatalogFactoryTests
{
    [Test]
    public void ShouldProjectLegacyAbilityMetadataIntoImmutableSnapshots()
    {
        var skill = new SkillMetadata
        {
            Name = "Assail",
            ManaCost = 12,
            Cooldown = TimeSpan.FromSeconds(3),
            IsAssail = true,
            OpensDialog = true,
            RequiresDisarm = true,
            MinHealthPercent = 20,
            MaxHealthPercent = 80
        };
        var spell = new SpellMetadata
        {
            Name = "Mor Dion",
            NumberOfLines = 4,
            ManaCost = 55,
            Cooldown = TimeSpan.FromSeconds(8)
        };

        var catalog = AbilitySnapshotCatalogFactory.Create(
            [skill],
            [spell]);
        var projectedSkill = catalog.FindSkill(skill.Name);
        var projectedSpell = catalog.FindSpell(spell.Name);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.SkillCount, Is.EqualTo(1));
            Assert.That(catalog.SpellCount, Is.EqualTo(1));
            Assert.That(projectedSkill?.ManaCost, Is.EqualTo(12));
            Assert.That(
                projectedSkill?.Cooldown,
                Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(projectedSkill?.IsAssail, Is.True);
            Assert.That(projectedSkill?.OpensDialog, Is.True);
            Assert.That(projectedSkill?.RequiresDisarm, Is.True);
            Assert.That(
                projectedSkill?.HealthCondition
                    .MinimumPercentExclusive,
                Is.EqualTo(20));
            Assert.That(
                projectedSkill?.HealthCondition
                    .MaximumPercentInclusive,
                Is.EqualTo(80));
            Assert.That(projectedSpell?.CastLines, Is.EqualTo(4));
            Assert.That(projectedSpell?.ManaCost, Is.EqualTo(55));
            Assert.That(
                projectedSpell?.Cooldown,
                Is.EqualTo(TimeSpan.FromSeconds(8)));
        });
    }

    [Test]
    public void ShouldTreatZeroLegacyHealthLimitsAsUnrestricted()
    {
        var catalog = AbilitySnapshotCatalogFactory.Create(
            [
                new SkillMetadata
                {
                    Name = "Unrestricted",
                    MinHealthPercent = 0,
                    MaxHealthPercent = 0
                }
            ],
            Array.Empty<SpellMetadata>());

        Assert.That(
            catalog.FindSkill("unrestricted")
                ?.HealthCondition.IsRestricted,
            Is.False);
    }
}
