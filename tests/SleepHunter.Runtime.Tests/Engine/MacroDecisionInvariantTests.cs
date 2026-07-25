using System.Collections.Immutable;

using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Tests.Hosting;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class MacroDecisionInvariantTests
{
    [Test]
    public void ShouldRejectClientIntentWithoutMatchingPendingAction()
    {
        var pendingIntent =
            new TestClientActionIntent(new ClientActionId(1));
        var emittedIntent =
            new TestClientActionIntent(new ClientActionId(2));
        var state = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            new PendingAction(
                pendingIntent,
                MacroTimestamp.Zero,
                new MacroTimestamp(TimeSpan.FromSeconds(1)),
                attempt: 1));
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            emittedIntent,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectScheduledEventEarlierThanCurrentTime()
    {
        var decision = new MacroDecision(
            MacroState.Initial,
            ImmutableArray<MacroEvent>.Empty,
            [
                new ScheduledMacroEvent(
                    new TestDeadlineEvent(1),
                    MacroTimestamp.Zero)
            ],
            intent: null,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                MacroState.Initial,
                decision,
                new MacroTimestamp(TimeSpan.FromTicks(1))),
            Throws.TypeOf<InvalidOperationException>());
    }
}
