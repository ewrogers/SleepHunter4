using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Hosting;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Actions;

public sealed class PendingActionTests
{
    [Test]
    public void ShouldRequireBoundedPendingActionMetadata()
    {
        var intent = new TestClientActionIntent(new ClientActionId(1));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => new PendingAction(
                    intent,
                    MacroTimestamp.Zero,
                    MacroTimestamp.Zero,
                    attempt: 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new PendingAction(
                    intent,
                    MacroTimestamp.Zero,
                    new MacroTimestamp(TimeSpan.FromSeconds(1)),
                    attempt: 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ShouldClearPendingActionWhenPaused()
    {
        var engine = new MacroEngine();
        var client = new ClientIdentity("client", "test");
        var snapshot = new ClientSnapshot(
            new SnapshotSequence(1),
            MacroTimestamp.Zero,
            MacroTimestamp.Zero,
            client,
            SnapshotQuality.Complete,
            ClientPresence.InWorld);
        var intent = new TestClientActionIntent(new ClientActionId(1));
        var pendingAction = new PendingAction(
            intent,
            MacroTimestamp.Zero,
            new MacroTimestamp(TimeSpan.FromSeconds(1)),
            attempt: 1);
        var currentState = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            snapshot,
            MacroTimestamp.Zero,
            pendingAction);

        var decision = engine.Decide(
            currentState,
            new MacroCommandReceived(new PauseMacroCommand()),
            MacroTimestamp.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(decision.State.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(decision.State.PendingAction, Is.Null);
            Assert.That(decision.PublishedView?.PendingActionId, Is.Null);
        });
    }
}
