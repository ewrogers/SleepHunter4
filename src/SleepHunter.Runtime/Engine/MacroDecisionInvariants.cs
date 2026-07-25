using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

internal static class MacroDecisionInvariants
{
    public static void EnsureValid(
        MacroState previousState,
        MacroDecision decision,
        MacroTimestamp currentTime)
    {
        if (decision.State.Revision < previousState.Revision)
        {
            throw new InvalidOperationException("State revisions cannot move backward.");
        }

        var stateChanged = decision.State.Revision != previousState.Revision;
        if (stateChanged != (decision.PublishedView is not null))
        {
            throw new InvalidOperationException(
                "Every state revision must publish exactly one matching view.");
        }

        if (decision.PublishedView is not null &&
            decision.PublishedView.Revision != decision.State.Revision)
        {
            throw new InvalidOperationException(
                "Published view revision must match the state revision.");
        }

        if (decision.Intent is ClientActionIntent clientActionIntent)
        {
            if (decision.State.Lifecycle != MacroLifecycle.Running)
            {
                throw new InvalidOperationException(
                    "Client action intents are only allowed while the macro is running.");
            }

            if (decision.State.PendingAction?.Intent.ActionId !=
                clientActionIntent.ActionId)
            {
                throw new InvalidOperationException(
                    "Client action intents require matching bounded pending action state.");
            }

            var pendingAction = decision.State.PendingAction!;
            var matchingDeadlines = decision.ScheduledEvents.Count(
                scheduledEvent =>
                    scheduledEvent.Input is ClientActionDeadlineElapsed deadline &&
                    deadline.ActionId == clientActionIntent.ActionId &&
                    scheduledEvent.DueAt == pendingAction.Deadline);

            if (matchingDeadlines != 1)
            {
                throw new InvalidOperationException(
                    "Client action intents require exactly one matching deadline event.");
            }
        }

        var pendingSwitchIntent =
            decision.State.PendingAction?.Intent as SwitchPanelIntent;
        var pendingPanelTransition = decision.State.PanelTransition is
        {
            Status: PanelTransitionStatus.Pending
        };

        if ((pendingSwitchIntent is not null) != pendingPanelTransition)
        {
            throw new InvalidOperationException(
                "Pending panel transition state must match its client action.");
        }

        if (pendingSwitchIntent is not null &&
            (decision.State.PanelTransition!.ActionId !=
             pendingSwitchIntent.ActionId ||
             decision.State.PanelTransition.TargetPanel !=
             pendingSwitchIntent.TargetPanel ||
             decision.State.PanelTransition.Attempt !=
             decision.State.PendingAction!.Attempt ||
             decision.State.PanelTransition.MaximumAttempts !=
             decision.State.PendingAction.MaximumAttempts))
        {
            throw new InvalidOperationException(
                "Pending panel transition metadata must match its client action.");
        }

        if (decision.ScheduledEvents.Any(
                scheduledEvent => scheduledEvent.DueAt < currentTime))
        {
            throw new InvalidOperationException(
                "Scheduled events cannot be earlier than the current time.");
        }
    }
}
