using System.Collections.Immutable;

using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Hosting;

internal sealed record TestMacroCommand(int Value) : MacroCommand;

internal sealed record TestDeadlineEvent(int Value) : MacroEvent;

internal sealed record TestClientActionIntent : ClientActionIntent
{
    public TestClientActionIntent(ClientActionId actionId)
        : base(actionId)
    {
    }
}

internal sealed class CountingMacroEngine : IMacroEngine
{
    private readonly List<int> receivedCommands = [];

    public IReadOnlyList<int> ReceivedCommands => receivedCommands;

    public MacroDecision Decide(
        MacroState currentState,
        MacroEvent input,
        MacroTimestamp currentTime)
    {
        if (input is not MacroCommandReceived
            {
                Command: TestMacroCommand command
            })
        {
            return TestMacroDecisions.Unchanged(currentState);
        }

        receivedCommands.Add(command.Value);
        return TestMacroDecisions.Changed(currentState, currentTime);
    }
}

internal sealed class SchedulingMacroEngine : IMacroEngine
{
    private readonly TimeSpan delay;
    private readonly TaskCompletionSource processed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource scheduled =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SchedulingMacroEngine(TimeSpan delay)
    {
        this.delay = delay;
    }

    public Task Scheduled => scheduled.Task;

    public MacroTimestamp? ProcessedAt { get; private set; }

    public Task Processed => processed.Task;

    public MacroDecision Decide(
        MacroState currentState,
        MacroEvent input,
        MacroTimestamp currentTime)
    {
        switch (input)
        {
            case MacroCommandReceived
            {
                Command: TestMacroCommand command
            }:
                scheduled.TrySetResult();
                return TestMacroDecisions.Unchanged(
                    currentState,
                    scheduledEvents:
                    [
                        new ScheduledMacroEvent(
                            new TestDeadlineEvent(command.Value),
                            currentTime.Add(delay))
                    ]);

            case TestDeadlineEvent:
                ProcessedAt = currentTime;
                processed.TrySetResult();
                return TestMacroDecisions.Changed(currentState, currentTime);

            default:
                return TestMacroDecisions.Unchanged(currentState);
        }
    }
}

internal sealed class IntentMacroEngine : IMacroEngine
{
    public MacroDecision Decide(
        MacroState currentState,
        MacroEvent input,
        MacroTimestamp currentTime)
    {
        if (input is not MacroCommandReceived
            {
                Command: TestMacroCommand command
            })
        {
            return TestMacroDecisions.Unchanged(currentState);
        }

        var intent = new TestClientActionIntent(
            new ClientActionId(command.Value));
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            currentTime.Add(TimeSpan.FromSeconds(1)),
            attempt: 1);
        var nextState = new MacroState(
            checked(currentState.Revision + 1),
            MacroLifecycle.Running,
            MacroStopReason.None,
            currentState.LatestSnapshot,
            currentTime,
            pendingAction);

        return new MacroDecision(
            nextState,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent,
            MacroViewSnapshot.FromState(nextState));
    }
}

internal static class TestMacroDecisions
{
    public static MacroDecision Unchanged(
        MacroState state,
        ImmutableArray<ScheduledMacroEvent> scheduledEvents = default)
    {
        if (scheduledEvents.IsDefault)
        {
            scheduledEvents = ImmutableArray<ScheduledMacroEvent>.Empty;
        }

        return new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            scheduledEvents,
            intent: null,
            publishedView: null);
    }

    public static MacroDecision Changed(
        MacroState currentState,
        MacroTimestamp currentTime)
    {
        var nextState = new MacroState(
            checked(currentState.Revision + 1),
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentTime,
            currentState.PendingAction);

        return new MacroDecision(
            nextState,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            MacroViewSnapshot.FromState(nextState));
    }
}
