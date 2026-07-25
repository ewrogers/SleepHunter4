using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.WaterBeds;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine
{
    private static MacroDecision UseWaterBed(
        MacroState currentState,
        UseWaterBedCommand command,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not null ||
            currentState.LatestSnapshot is not
            {
                Presence: ClientPresence.InWorld
            } snapshot)
        {
            return Unchanged(currentState);
        }

        var snapshotRequiredAfter =
            currentState.WaterBed?.SnapshotRequiredAfter;
        var snapshotIsFresh =
            snapshotRequiredAfter is null ||
            snapshot.CaptureStartedAt > snapshotRequiredAfter;
        var plan = WaterBedPlanner.Plan(
            new WaterBedPlanningRequest(
                snapshot.Location,
                snapshot.Vitals,
                currentState.WaterBed?.ReadyAt,
                currentTime,
                command.Policy,
                snapshotIsFresh));
        var waterBed = WaterBedState.FromPlan(
            plan,
            command.Policy,
            snapshotRequiredAfter);

        if (!plan.IsReady)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                waterBed: waterBed);
        }

        var actionId = new ClientActionId(
            currentState.NextClientActionId);
        var intent = new ClickTileIntent(actionId, plan.Target!);
        var deadline = currentTime.Add(command.Policy.ActionDuration);
        var readyAt = currentTime.Add(command.Policy.MinimumInterval);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            snapshot.Sequence);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            waterBed: waterBed.Clicking(actionId, deadline, readyAt),
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static MacroDecision HandleClickTileDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        ClickTileIntent intent)
    {
        if (currentState.WaterBed is not
            {
                Status: WaterBedStatus.Clicking
            } waterBed ||
            waterBed.ActionId != intent.ActionId ||
            waterBed.CompletesAt != pendingAction.Deadline ||
            waterBed.Plan.Target != intent.Target)
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            pendingAction: null,
            waterBed: waterBed.Succeeded());
    }

    private static WaterBedState? CancelPendingWaterBed(
        MacroState currentState) =>
        currentState.WaterBed is
        {
            Status: WaterBedStatus.Clicking
        } waterBed
            ? waterBed.Cancelled()
            : currentState.WaterBed;
}
