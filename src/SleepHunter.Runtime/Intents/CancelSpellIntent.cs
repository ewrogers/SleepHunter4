using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public sealed record CancelSpellIntent : ClientActionIntent
{
    public CancelSpellIntent(ClientActionId actionId)
        : base(actionId)
    {
    }
}
