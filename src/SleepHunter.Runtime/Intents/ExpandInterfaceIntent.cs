using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public sealed record ExpandInterfaceIntent : ClientActionIntent
{
    public ExpandInterfaceIntent(ClientActionId actionId)
        : base(actionId)
    {
    }
}
