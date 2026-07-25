using SleepHunter.Runtime.Effects;
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

        if (decision.Effect is ClientActionEffect &&
            decision.State.Lifecycle != MacroLifecycle.Running)
        {
            throw new InvalidOperationException(
                "Client actions are only allowed while the macro is running.");
        }

        if (decision.NextDeadline is { } deadline && deadline < currentTime)
        {
            throw new InvalidOperationException(
                "The next deadline cannot be earlier than the current time.");
        }
    }
}
