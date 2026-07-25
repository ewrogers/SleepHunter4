using SleepHunter.Runtime.Automation.Flowering;

namespace SleepHunter.Runtime.Events;

public sealed record FlowerClientsObserved : MacroEvent
{
    public FlowerClientsObserved(FlowerClientSetSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
    }

    public FlowerClientSetSnapshot Snapshot { get; }
}
