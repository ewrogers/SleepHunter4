using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.WaterBeds;

public sealed record WaterBedPlan
{
    internal WaterBedPlan(
        WaterBedPlanStatus status,
        MapLocationSnapshot? target,
        MacroTimestamp? readyAt)
    {
        Status = status;
        Target = target;
        ReadyAt = readyAt;
    }

    public WaterBedPlanStatus Status { get; }

    public MapLocationSnapshot? Target { get; }

    public MacroTimestamp? ReadyAt { get; }

    public bool IsReady => Status == WaterBedPlanStatus.Ready;
}
