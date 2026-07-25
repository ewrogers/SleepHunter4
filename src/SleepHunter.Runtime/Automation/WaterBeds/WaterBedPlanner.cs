using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.WaterBeds;

public static class WaterBedPlanner
{
    public static WaterBedPlan Plan(WaterBedPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.SnapshotIsFresh ||
            request.Location is not { } location ||
            request.Vitals is not { } vitals)
        {
            return new WaterBedPlan(
                WaterBedPlanStatus.SnapshotUnavailable,
                target: null,
                request.ReadyAt);
        }

        var target = new MapLocationSnapshot(
            location.MapNumber,
            location.MapName,
            request.Policy.TargetX,
            request.Policy.TargetY);

        if (vitals.CurrentMana >= request.Policy.ManaThreshold)
        {
            return new WaterBedPlan(
                WaterBedPlanStatus.ManaSufficient,
                target,
                request.ReadyAt);
        }

        if (request.ReadyAt is { } readyAt &&
            readyAt > request.CurrentTime)
        {
            return new WaterBedPlan(
                WaterBedPlanStatus.CoolingDown,
                target,
                readyAt);
        }

        if (!location.IsWithinRange(
                target,
                request.Policy.MaximumXDistance,
                request.Policy.MaximumYDistance))
        {
            return new WaterBedPlan(
                WaterBedPlanStatus.OutOfRange,
                target,
                request.ReadyAt);
        }

        return new WaterBedPlan(
            WaterBedPlanStatus.Ready,
            target,
            request.ReadyAt);
    }
}
