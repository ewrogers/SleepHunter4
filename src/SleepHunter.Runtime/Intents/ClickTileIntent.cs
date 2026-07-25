using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Intents;

public sealed record ClickTileIntent : ClientActionIntent
{
    public ClickTileIntent(
        ClientActionId actionId,
        MapLocationSnapshot target)
        : base(actionId)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    public MapLocationSnapshot Target { get; }
}
