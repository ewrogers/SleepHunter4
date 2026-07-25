using System.Collections.Immutable;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed class MacroEngine : IMacroEngine
{
    public MacroDecision Decide(
        MacroState currentState,
        MacroEvent input,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentNullException.ThrowIfNull(input);

        var decision = input switch
        {
            MacroCommandReceived commandReceived =>
                HandleCommand(currentState, commandReceived.Command, currentTime),
            ClientSnapshotObserved snapshotObserved =>
                HandleSnapshot(currentState, snapshotObserved.Snapshot, currentTime),
            _ => throw new ArgumentOutOfRangeException(
                nameof(input),
                input,
                "Unsupported macro event.")
        };

        MacroDecisionInvariants.EnsureValid(currentState, decision, currentTime);
        return decision;
    }

    private static MacroDecision HandleCommand(
        MacroState currentState,
        MacroCommand command,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            StartMacroCommand => Start(currentState, currentTime),
            PauseMacroCommand => ChangeLifecycle(
                currentState,
                MacroLifecycle.Running,
                MacroLifecycle.Paused,
                MacroStopReason.None,
                currentTime),
            ResumeMacroCommand => ChangeLifecycle(
                currentState,
                MacroLifecycle.Paused,
                MacroLifecycle.Running,
                MacroStopReason.None,
                currentTime),
            StopMacroCommand => Stop(currentState, currentTime),
            _ => throw new ArgumentOutOfRangeException(
                nameof(command),
                command,
                "Unsupported macro command.")
        };
    }

    private static MacroDecision Start(
        MacroState currentState,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Stopped)
        {
            return Unchanged(currentState);
        }

        if (currentState.LatestSnapshot is not { Presence: ClientPresence.InWorld })
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            MacroLifecycle.Running,
            MacroStopReason.None,
            currentState.LatestSnapshot,
            currentTime,
            pendingAction: null);
    }

    private static MacroDecision Stop(
        MacroState currentState,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle == MacroLifecycle.Stopped)
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            MacroLifecycle.Stopped,
            MacroStopReason.UserRequested,
            currentState.LatestSnapshot,
            currentTime,
            pendingAction: null);
    }

    private static MacroDecision ChangeLifecycle(
        MacroState currentState,
        MacroLifecycle requiredLifecycle,
        MacroLifecycle nextLifecycle,
        MacroStopReason stopReason,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != requiredLifecycle)
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            nextLifecycle,
            stopReason,
            currentState.LatestSnapshot,
            currentTime,
            pendingAction: nextLifecycle == MacroLifecycle.Running
                ? currentState.PendingAction
                : null);
    }

    private static MacroDecision HandleSnapshot(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.IsUsable || snapshot.CaptureCompletedAt > currentTime)
        {
            return Unchanged(currentState);
        }

        if (currentState.LatestSnapshot is { } latestSnapshot &&
            (snapshot.Client != latestSnapshot.Client ||
             snapshot.Sequence <= latestSnapshot.Sequence))
        {
            return Unchanged(currentState);
        }

        var clientLoggedOut =
            snapshot.Presence != ClientPresence.InWorld &&
            currentState.Lifecycle != MacroLifecycle.Stopped;

        var lifecycle = clientLoggedOut
            ? MacroLifecycle.Stopped
            : currentState.Lifecycle;
        var stopReason = clientLoggedOut
            ? MacroStopReason.ClientLoggedOut
            : currentState.StopReason;
        var lastTransitionAt = clientLoggedOut
            ? currentTime
            : currentState.LastTransitionAt;
        var pendingAction = clientLoggedOut
            ? null
            : currentState.PendingAction;

        return Changed(
            currentState,
            lifecycle,
            stopReason,
            snapshot,
            lastTransitionAt,
            pendingAction);
    }

    private static MacroDecision Changed(
        MacroState currentState,
        MacroLifecycle lifecycle,
        MacroStopReason stopReason,
        ClientSnapshot? latestSnapshot,
        MacroTimestamp? lastTransitionAt,
        PendingAction? pendingAction)
    {
        var nextState = new MacroState(
            checked(currentState.Revision + 1),
            lifecycle,
            stopReason,
            latestSnapshot,
            lastTransitionAt,
            pendingAction);

        return new MacroDecision(
            nextState,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            MacroViewSnapshot.FromState(nextState));
    }

    private static MacroDecision Unchanged(MacroState currentState) =>
        new(
            currentState,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            publishedView: null);
}
