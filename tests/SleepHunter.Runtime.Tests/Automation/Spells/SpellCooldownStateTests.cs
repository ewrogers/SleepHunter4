using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Automation.Spells;

public sealed class SpellCooldownStateTests
{
    [Test]
    public void ShouldUseCaseInsensitiveExclusiveCooldownBoundary()
    {
        var readyAt = new MacroTimestamp(TimeSpan.FromSeconds(5));
        var state = SpellCooldownState.Empty.WithCooldown("  Test Spell  ", readyAt);

        Assert.Multiple(() =>
        {
            Assert.That(
                state.GetReadyAt(
                    "test spell",
                    new MacroTimestamp(TimeSpan.FromSeconds(4))),
                Is.EqualTo(readyAt));
            Assert.That(
                state.GetReadyAt("TEST SPELL", readyAt),
                Is.Null);
            Assert.That(
                state.Prune(readyAt),
                Is.EqualTo(SpellCooldownState.Empty));
        });
    }

    [Test]
    public void ShouldReplaceAndClearCooldownBySpellName()
    {
        var state = SpellCooldownState.Empty
            .WithCooldown(
                "spell",
                new MacroTimestamp(TimeSpan.FromSeconds(1)))
            .WithCooldown(
                "SPELL",
                new MacroTimestamp(TimeSpan.FromSeconds(2)));

        Assert.Multiple(() =>
        {
            Assert.That(state.ReadyAtBySpell, Has.Count.EqualTo(1));
            Assert.That(
                state.GetReadyAt("spell", MacroTimestamp.Zero),
                Is.EqualTo(new MacroTimestamp(TimeSpan.FromSeconds(2))));
            Assert.That(
                state.Clear("SpElL"),
                Is.EqualTo(SpellCooldownState.Empty));
        });
    }

    [Test]
    public void ShouldCompareIndependentCooldownStatesByValue()
    {
        var first = SpellCooldownState.Empty
            .WithCooldown(
                "first",
                new MacroTimestamp(TimeSpan.FromSeconds(1)))
            .WithCooldown(
                "second",
                new MacroTimestamp(TimeSpan.FromSeconds(2)));
        var second = SpellCooldownState.Empty
            .WithCooldown(
                "SECOND",
                new MacroTimestamp(TimeSpan.FromSeconds(2)))
            .WithCooldown(
                "FIRST",
                new MacroTimestamp(TimeSpan.FromSeconds(1)));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        });
    }
}
