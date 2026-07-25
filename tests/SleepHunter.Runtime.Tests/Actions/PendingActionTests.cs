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
            Assert.That(
                () => new PendingAction(
                    intent,
                    MacroTimestamp.Zero,
                    new MacroTimestamp(TimeSpan.FromSeconds(1)),
                    attempt: 2,
                    maximumAttempts: 1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ShouldRecordIssuanceWithoutMutatingTheRequest()
    {
        var intent = new TestClientActionIntent(new ClientActionId(1));
        var requestedAt = new MacroTimestamp(TimeSpan.FromMilliseconds(100));
        var issuedAt = new MacroTimestamp(TimeSpan.FromMilliseconds(125));
        var pendingAction = new PendingAction(
            intent,
            requestedAt,
            new MacroTimestamp(TimeSpan.FromSeconds(1)),
            attempt: 2,
            maximumAttempts: 3,
            new SnapshotSequence(7));

        var issuedAction = pendingAction.MarkIssued(issuedAt);

        Assert.Multiple(() =>
        {
            Assert.That(pendingAction.IsIssued, Is.False);
            Assert.That(pendingAction.IssuedAt, Is.Null);
            Assert.That(issuedAction.IsIssued, Is.True);
            Assert.That(issuedAction.IssuedAt, Is.EqualTo(issuedAt));
            Assert.That(issuedAction.RequestedAt, Is.EqualTo(requestedAt));
            Assert.That(issuedAction.Intent, Is.SameAs(intent));
            Assert.That(issuedAction.Attempt, Is.EqualTo(2));
            Assert.That(issuedAction.MaximumAttempts, Is.EqualTo(3));
            Assert.That(
                issuedAction.BaselineSnapshotSequence,
                Is.EqualTo(new SnapshotSequence(7)));
        });
    }

    [Test]
    public void ShouldRejectIssuanceBeforeTheRequest()
    {
        var pendingAction = new PendingAction(
            new TestClientActionIntent(new ClientActionId(1)),
            new MacroTimestamp(TimeSpan.FromMilliseconds(100)),
            new MacroTimestamp(TimeSpan.FromSeconds(1)),
            attempt: 1);

        Assert.That(
            () => pendingAction.MarkIssued(
                new MacroTimestamp(TimeSpan.FromMilliseconds(99))),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ShouldClearPendingActionWhenPaused()
    {
        var engine = new MacroEngine();
        var client = new ClientIdentity("client");
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
