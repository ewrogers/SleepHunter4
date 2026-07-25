using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public abstract record MacroIntent;

public abstract record ClientActionIntent : MacroIntent
{
    protected ClientActionIntent(ClientActionId actionId)
    {
        if (actionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionId),
                actionId,
                "Client action intents require a valid action identifier.");
        }

        ActionId = actionId;
    }

    public ClientActionId ActionId { get; }
}
