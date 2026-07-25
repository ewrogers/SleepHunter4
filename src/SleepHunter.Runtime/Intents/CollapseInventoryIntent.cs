using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public sealed record CollapseInventoryIntent : ClientActionIntent
{
    public CollapseInventoryIntent(ClientActionId actionId)
        : base(actionId)
    {
    }
}
