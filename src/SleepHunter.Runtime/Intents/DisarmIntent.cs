using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public sealed record DisarmIntent : ClientActionIntent
{
    public DisarmIntent(ClientActionId actionId)
        : base(actionId)
    {
    }
}
