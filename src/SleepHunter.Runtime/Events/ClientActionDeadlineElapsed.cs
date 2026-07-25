using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Events;

public sealed record ClientActionDeadlineElapsed : MacroEvent
{
    public ClientActionDeadlineElapsed(ClientActionId actionId)
    {
        if (actionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionId),
                actionId,
                "Client action deadlines require a valid action identifier.");
        }

        ActionId = actionId;
    }

    public ClientActionId ActionId { get; }
}
