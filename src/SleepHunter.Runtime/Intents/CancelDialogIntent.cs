using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public sealed record CancelDialogIntent : ClientActionIntent
{
    public CancelDialogIntent(ClientActionId actionId)
        : base(actionId)
    {
    }
}
