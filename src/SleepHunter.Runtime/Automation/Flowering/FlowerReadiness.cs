using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerReadiness(
    FlowerQueueEntry Entry,
    FlowerClientObservation? TargetClient,
    FlowerReadinessStatus Status,
    MacroTimestamp? ReadyAt);
