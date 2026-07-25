namespace SleepHunter.Runtime.Tests.Automation.Spells;

using SleepHunter.Runtime.Automation.Spells;

public sealed class TargetRotationStateTests
{
    [Test]
    public void ShouldTrackEntriesIndependentlyAndPreserveUnchangedTargets()
    {
        var first = SpellTarget.RelativeArea(0, 0, 0, 1);
        var second = SpellTarget.AbsoluteArea(20, 20, 1, 2);
        var state = TargetRotationState.Empty.Synchronize(
        [
            KeyValuePair.Create(1L, first),
            KeyValuePair.Create(2L, second)
        ]);

        var firstResolution = state.Resolve(1, first);
        state = state.Advance(1, first, firstResolution);
        var synchronized = state.Synchronize(
        [
            KeyValuePair.Create(2L, second),
            KeyValuePair.Create(1L, first)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(synchronized.Count, Is.EqualTo(2));
            Assert.That(synchronized.GetCursor(1), Is.EqualTo(1));
            Assert.That(synchronized.GetCursor(2), Is.Zero);
            Assert.That(synchronized, Is.EqualTo(state));
        });
    }

    [Test]
    public void ShouldResetChangedTargetAndRemoveMissingOrNonAreaEntries()
    {
        var original = SpellTarget.RelativeArea(0, 0, 0, 1);
        var state = TargetRotationState.Empty.Synchronize(
            [KeyValuePair.Create(1L, original)]);
        state = state.Advance(1, original, state.Resolve(1, original));

        var changed = SpellTarget.RelativeArea(1, 0, 0, 1);
        var reset = state.Synchronize(
            [KeyValuePair.Create(1L, changed)]);
        var removed = reset.Synchronize(
            [KeyValuePair.Create(1L, SpellTarget.Self)]);

        Assert.Multiple(() =>
        {
            Assert.That(state.GetCursor(1), Is.EqualTo(1));
            Assert.That(reset.GetCursor(1), Is.Zero);
            Assert.That(removed.Count, Is.Zero);
        });
    }
}
