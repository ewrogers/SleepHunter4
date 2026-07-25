using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.WaterBeds;

public sealed record WaterBedPlanningRequest
{
    public WaterBedPlanningRequest(
        MapLocationSnapshot? location,
        VitalsSnapshot? vitals,
        MacroTimestamp? readyAt,
        MacroTimestamp currentTime,
        WaterBedPolicy policy,
        bool snapshotIsFresh = true)
    {
        ArgumentNullException.ThrowIfNull(policy);

        Location = location;
        Vitals = vitals;
        ReadyAt = readyAt;
        CurrentTime = currentTime;
        Policy = policy;
        SnapshotIsFresh = snapshotIsFresh;
    }

    public MapLocationSnapshot? Location { get; }

    public VitalsSnapshot? Vitals { get; }

    public MacroTimestamp? ReadyAt { get; }

    public MacroTimestamp CurrentTime { get; }

    public WaterBedPolicy Policy { get; }

    public bool SnapshotIsFresh { get; }
}
