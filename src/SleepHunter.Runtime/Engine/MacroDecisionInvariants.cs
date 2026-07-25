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
        }

        if (decision.ScheduledEvents.Any(
                scheduledEvent => scheduledEvent.DueAt < currentTime))
        {
            throw new InvalidOperationException(
                "Scheduled events cannot be earlier than the current time.");
        }
    }
}
