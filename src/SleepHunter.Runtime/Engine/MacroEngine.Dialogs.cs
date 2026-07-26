using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine
{
    private static readonly TimeSpan DialogArbitrationDelay =
        TimeSpan.FromTicks(1);

    private static MacroDecision HandleDialogCloseDue(
        MacroState currentState,
        DialogCloseDue closeDue,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.Dialog is not
            {
                Status: DialogStatus.Scheduled,
                DueAt: { } dueAt
            } dialog ||
            dueAt != closeDue.DueAt ||
            currentTime < dueAt)
        {
            return Unchanged(currentState);
        }

        if (currentState.LatestSnapshot is not { IsPopupOpen: true })
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                currentState.PendingAction,
                dialog: dialog.Closed());
        }

        if (currentState.PendingAction is { } pendingAction)
        {
            var afterCurrentTime = currentTime.Add(DialogArbitrationDelay);
            var afterPendingAction =
                pendingAction.Deadline.Add(DialogArbitrationDelay);
            var nextDueAt = afterCurrentTime >= afterPendingAction
                ? afterCurrentTime
                : afterPendingAction;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction,
                dialog: dialog.Rescheduled(nextDueAt),
                scheduledEvents:
                [
                    new ScheduledMacroEvent(
                        new DialogCloseDue(nextDueAt),
                        nextDueAt)
                ]);
        }

        return RequestDialogClose(
            currentState,
            dialog,
            currentState.LatestSnapshot,
            currentTime);
    }

    private static MacroDecision HandleDialogCloseDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        CancelDialogIntent intent,
        MacroTimestamp currentTime)
    {
        if (currentState.Dialog is not
            {
                Status: DialogStatus.Closing
            } dialog ||
            dialog.ActionId != intent.ActionId ||
            dialog.CompletesAt != pendingAction.Deadline)
        {
            return Unchanged(currentState);
        }

        var awaitingObservation = dialog.AwaitingObservation(
            pendingAction.IssuedAt ?? pendingAction.RequestedAt);
        if (currentState.LatestSnapshot is not { } latestSnapshot ||
            !CanSnapshotConfirmAction(pendingAction, latestSnapshot))
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                dialog: awaitingObservation);
        }

        return latestSnapshot.IsPopupOpen
            ? RequestDialogClose(
                currentState,
                awaitingObservation,
                latestSnapshot,
                currentTime)
            : Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                latestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                dialog: awaitingObservation.Closed());
    }

    private static MacroDecision ObservePendingDialog(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        if (currentState.PendingAction is not null ||
            currentState.Dialog is not { } dialog)
        {
            return Unchanged(currentState);
        }

        if (dialog.Status == DialogStatus.Scheduled)
        {
            return snapshot.IsPopupOpen
                ? RequestDialogClose(
                    currentState,
                    dialog,
                    snapshot,
                    currentTime)
                : Unchanged(currentState);
        }

        if (dialog.Status != DialogStatus.AwaitingObservation ||
            dialog.LastCancelSnapshotSequence is not { } baseline ||
            dialog.SnapshotRequiredAfter is not { } requiredAfter ||
            snapshot.Sequence <= baseline ||
            snapshot.CaptureStartedAt <= requiredAfter)
        {
            return Unchanged(currentState);
        }

        return snapshot.IsPopupOpen
            ? RequestDialogClose(
                currentState,
                dialog,
                snapshot,
                currentTime)
            : Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                dialog: dialog.Closed());
    }

    private static MacroDecision RequestDialogClose(
        MacroState currentState,
        DialogState dialog,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = new CancelDialogIntent(actionId);
        var deadline = currentTime.Add(dialog.Policy.ActionDuration);
        var closeAction = new PendingAction(
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
            closeAction,
            dialog: dialog.Closing(
                actionId,
                deadline,
                snapshot.Sequence),
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static DialogState? CancelPendingDialog(
        MacroState currentState) =>
        currentState.Dialog is
        {
            Status:
                DialogStatus.Scheduled or
                DialogStatus.Closing or
                DialogStatus.AwaitingObservation
        } dialog
            ? dialog.Cancelled()
            : currentState.Dialog;
}
