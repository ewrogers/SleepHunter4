using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public sealed record ExpandInventoryIntent : ClientActionIntent
{
    public ExpandInventoryIntent(ClientActionId actionId)
        : base(actionId)
    {
    }
}
