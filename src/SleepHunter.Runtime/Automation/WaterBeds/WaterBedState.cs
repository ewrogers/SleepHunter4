using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.WaterBeds;

public sealed record WaterBedState
{
    private WaterBedState(
        WaterBedPlan plan,
        WaterBedPolicy policy,
        WaterBedStatus status,
        MacroTimestamp? readyAt,
        MacroTimestamp? snapshotRequiredAfter,
        ClientActionId? actionId,
        MacroTimestamp? completesAt)
    {
        Plan = plan;
        Policy = policy;
        Status = status;
        ReadyAt = readyAt;
        SnapshotRequiredAfter = snapshotRequiredAfter;
        ActionId = actionId;
        CompletesAt = completesAt;
    }

    public WaterBedPlan Plan { get; private init; }

    public WaterBedPolicy Policy { get; }

    public WaterBedStatus Status { get; private init; }

    public MacroTimestamp? ReadyAt { get; private init; }

    public MacroTimestamp? SnapshotRequiredAfter { get; private init; }

    public ClientActionId? ActionId { get; private init; }

    public MacroTimestamp? CompletesAt { get; private init; }

    internal static WaterBedState FromPlan(
        WaterBedPlan plan,
        WaterBedPolicy policy,
        MacroTimestamp? snapshotRequiredAfter)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);

        var status = plan.Status switch
        {
            WaterBedPlanStatus.SnapshotUnavailable =>
                WaterBedStatus.SnapshotUnavailable,
            WaterBedPlanStatus.ManaSufficient =>
                WaterBedStatus.ManaSufficient,
            WaterBedPlanStatus.CoolingDown =>
                WaterBedStatus.CoolingDown,
            WaterBedPlanStatus.OutOfRange =>
                WaterBedStatus.OutOfRange,
            WaterBedPlanStatus.Ready =>
                WaterBedStatus.Clicking,
            _ => throw new ArgumentOutOfRangeException(
                nameof(plan),
                plan.Status,
                "The water and bed plan status is not supported.")
        };
        return new WaterBedState(
            plan,
            policy,
            status,
            plan.ReadyAt,
            snapshotRequiredAfter,
            actionId: null,
            completesAt: null);
    }

    internal WaterBedState Clicking(
        ClientActionId actionId,
        MacroTimestamp completesAt,
        MacroTimestamp readyAt) =>
        this with
        {
            Status = WaterBedStatus.Clicking,
            ReadyAt = readyAt,
            SnapshotRequiredAfter = completesAt,
            ActionId = actionId,
            CompletesAt = completesAt
        };

    internal WaterBedState Succeeded() =>
        this with { Status = WaterBedStatus.Succeeded };

    internal WaterBedState Cancelled() =>
        this with { Status = WaterBedStatus.Cancelled };
}
