using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class SkillbookSnapshotTests
{
    [Test]
    public void ShouldValidateAndNormalizeSkillValues()
    {
        var skill = Skill(" skill ", slot: 37);

        Assert.Multiple(() =>
        {
            Assert.That(skill.Name, Is.EqualTo("skill"));
            Assert.That(skill.Panel, Is.EqualTo(ClientPanel.MedeniaSkills));
            Assert.That(
                Skill("world", slot: 73).Panel,
                Is.EqualTo(ClientPanel.WorldSkills));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = Skill("invalid", slot: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = Skill("invalid", manaCost: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = Skill(
                    "invalid",
                    cooldown: TimeSpan.FromTicks(-1)));
        });
    }

    [Test]
    public void ShouldSortFindAndCompareSkillbooksByValue()
    {
        var first = Skill("first", slot: 1);
        var second = Skill("second", slot: 2);
        var left = new SkillbookSnapshot([second, first]);
        var right = new SkillbookSnapshot(
        [
            Skill("first", slot: 1),
            Skill("second", slot: 2)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(left.Skills[0], Is.EqualTo(first));
            Assert.That(left.Find(" SECOND "), Is.EqualTo(second));
            Assert.That(left, Is.EqualTo(right));
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        });
    }

    [Test]
    public void ShouldRejectDuplicateSlotsAndNames()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = new SkillbookSnapshot(
                [
                    Skill("first", slot: 1),
                    Skill("second", slot: 1)
                ]));
            Assert.Throws<ArgumentException>(
                () => _ = new SkillbookSnapshot(
                [
                    Skill("skill", slot: 1),
                    Skill(" SKILL ", slot: 2)
                ]));
        });
    }

    private static SkillSnapshot Skill(
        string name,
        int slot = 1,
        int manaCost = 0,
        TimeSpan? cooldown = null) =>
        new(
            name,
            slot,
            currentLevel: 0,
            maximumLevel: 100,
            manaCost,
            cooldown ?? TimeSpan.Zero);
}
