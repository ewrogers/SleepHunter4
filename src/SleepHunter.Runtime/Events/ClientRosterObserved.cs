using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Events;

public sealed record ClientRosterObserved : MacroEvent
{
    public ClientRosterObserved(ClientRosterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
    }

    public ClientRosterSnapshot Snapshot { get; }
}
