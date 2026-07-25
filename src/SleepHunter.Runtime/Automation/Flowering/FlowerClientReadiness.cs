using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerClientReadiness(
    ClientRosterEntry Client,
    FlowerClientReadinessStatus Status);
