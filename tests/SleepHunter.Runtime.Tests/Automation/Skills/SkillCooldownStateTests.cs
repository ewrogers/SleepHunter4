using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Automation.Skills;

public sealed class SkillCooldownStateTests
{
    [Test]
    public void ShouldTrackNamesWithoutCaseSensitivity()
    {
        var readyAt = new MacroTimestamp(TimeSpan.FromSeconds(5));
        var state = SkillCooldownState.Empty.WithCooldown(
            " Skill ",
            readyAt);

        Assert.Multiple(() =>
        {
            Assert.That(
                state.GetReadyAt("skill", MacroTimestamp.Zero),
                Is.EqualTo(readyAt));
            Assert.That(
                state.WithCooldown("SKILL", readyAt),
                Is.SameAs(state));
            Assert.That(
                state.Clear("sKiLl"),
                Is.EqualTo(SkillCooldownState.Empty));
        });
    }

    [Test]
    public void ShouldPruneAtExactReadyTimeAndCompareByValue()
    {
        var readyAt = new MacroTimestamp(TimeSpan.FromSeconds(5));
        var first = SkillCooldownState.Empty
            .WithCooldown("first", readyAt)
            .WithCooldown(
                "second",
                new MacroTimestamp(TimeSpan.FromSeconds(6)));
        var second = SkillCooldownState.Empty
            .WithCooldown(
                "SECOND",
                new MacroTimestamp(TimeSpan.FromSeconds(6)))
            .WithCooldown("FIRST", readyAt);
        var pruned = first.Prune(readyAt);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(
                pruned.GetReadyAt("first", MacroTimestamp.Zero),
                Is.Null);
            Assert.That(
                pruned.GetReadyAt("second", MacroTimestamp.Zero),
                Is.Not.Null);
        });
    }
}
