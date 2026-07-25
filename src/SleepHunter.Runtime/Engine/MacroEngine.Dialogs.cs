using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
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

        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = new CancelDialogIntent(actionId);
        var deadline = currentTime.Add(dialog.Policy.ActionDuration);
        var closeAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            currentState.LatestSnapshot?.Sequence);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            closeAction,
            dialog: dialog.Closing(actionId, deadline),
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static MacroDecision HandleDialogCloseDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        CancelDialogIntent intent)
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

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            pendingAction: null,
            dialog: dialog.Closed());
    }

    private static DialogState? CancelPendingDialog(
        MacroState currentState) =>
        currentState.Dialog is
        {
            Status: DialogStatus.Scheduled or DialogStatus.Closing
        } dialog
            ? dialog.Cancelled()
            : currentState.Dialog;
}
