using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerReadiness(
    FlowerQueueEntry Entry,
    ClientRosterEntry? TargetClient,
    FlowerReadinessStatus Status,
    MacroTimestamp? ReadyAt);
