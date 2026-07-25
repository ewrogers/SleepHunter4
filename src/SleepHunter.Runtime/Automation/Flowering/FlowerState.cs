using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerState
{
    private FlowerState(
        FlowerPlan plan,
        FlowerExecutionPolicy policy,
        ClientRosterSequence? observationSequence,
        FlowerStatus status,
        FlowerActionKind? action,
        SpellQueueEntry? spellEntry,
        MacroTimestamp? floweredAt)
    {
        Plan = plan;
        Policy = policy;
        ObservationSequence = observationSequence;
        Status = status;
        Action = action;
        SpellEntry = spellEntry;
        FloweredAt = floweredAt;
    }

    public FlowerPlan Plan { get; private init; }

    public FlowerExecutionPolicy Policy { get; }

    public ClientRosterSequence? ObservationSequence { get; private init; }

    public FlowerStatus Status { get; private init; }

    public FlowerActionKind? Action { get; private init; }

    public SpellQueueEntry? SpellEntry { get; private init; }

    public MacroTimestamp? FloweredAt { get; private init; }

    internal static FlowerState FromPlan(
        FlowerPlan plan,
        FlowerExecutionPolicy policy,
        ClientRosterSequence? observationSequence)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);

        var status = plan.Status switch
        {
            FlowerPlanStatus.Idle => FlowerStatus.Idle,
            FlowerPlanStatus.Waiting => FlowerStatus.WaitingForTarget,
            FlowerPlanStatus.Ready => FlowerStatus.WaitingForPanel,
            _ => throw new ArgumentOutOfRangeException(
                nameof(plan),
                plan.Status,
                "The flower plan status is not supported.")
        };
        return new FlowerState(
            plan,
            policy,
            observationSequence,
            status,
            action: null,
            spellEntry: null,
            floweredAt: null);
    }

    internal FlowerState WithSpell(
        FlowerActionKind action,
        SpellQueueEntry spellEntry,
        SpellCastState spellCast) =>
        this with
        {
            Status = ToStatus(spellCast.Status),
            Action = action,
            SpellEntry = spellEntry
        };

    internal FlowerState WithPlan(
        FlowerPlan plan,
        ClientRosterSequence? observationSequence) =>
        this with
        {
            Plan = plan,
            ObservationSequence = observationSequence
        };

    internal FlowerState WithSpellCast(SpellCastState spellCast) =>
        this with { Status = ToStatus(spellCast.Status) };

    internal FlowerState SnapshotUnavailable() =>
        this with { Status = FlowerStatus.SnapshotUnavailable };

    internal FlowerState SpellUnavailable() =>
        this with { Status = FlowerStatus.SpellUnavailable };

    internal FlowerState Casting(MacroTimestamp? floweredAt) =>
        this with
        {
            Status = FlowerStatus.Casting,
            FloweredAt = floweredAt
        };

    internal FlowerState Succeeded(MacroTimestamp? floweredAt) =>
        this with
        {
            Status = FlowerStatus.Succeeded,
            FloweredAt = floweredAt ?? FloweredAt
        };

    internal FlowerState SelectionInvalidated(
        FlowerPlan plan,
        ClientRosterSequence? observationSequence) =>
        this with
        {
            Plan = plan,
            ObservationSequence = observationSequence,
            Status = FlowerStatus.SelectionInvalidated
        };

    internal FlowerState StaffUnavailable() =>
        this with { Status = FlowerStatus.StaffUnavailable };

    internal FlowerState PanelUnavailable() =>
        this with { Status = FlowerStatus.PanelUnavailable };

    internal FlowerState Cancelled() =>
        this with { Status = FlowerStatus.Cancelled };

    private static FlowerStatus ToStatus(SpellCastStatus status) =>
        status switch
        {
            SpellCastStatus.SnapshotUnavailable =>
                FlowerStatus.SnapshotUnavailable,
            SpellCastStatus.WaitingForMana =>
                FlowerStatus.WaitingForMana,
            SpellCastStatus.CoolingDown =>
                FlowerStatus.CoolingDown,
            SpellCastStatus.WaitingForStaff =>
                FlowerStatus.WaitingForStaff,
            SpellCastStatus.WaitingForPanel =>
                FlowerStatus.WaitingForPanel,
            SpellCastStatus.TargetUnavailable =>
                FlowerStatus.TargetUnavailable,
            SpellCastStatus.Casting =>
                FlowerStatus.Casting,
            SpellCastStatus.Succeeded =>
                FlowerStatus.Succeeded,
            SpellCastStatus.SelectionInvalidated =>
                FlowerStatus.SelectionInvalidated,
            SpellCastStatus.StaffUnavailable =>
                FlowerStatus.StaffUnavailable,
            SpellCastStatus.PanelUnavailable =>
                FlowerStatus.PanelUnavailable,
            SpellCastStatus.IssueFailed =>
                FlowerStatus.IssueFailed,
            SpellCastStatus.Cancelled =>
                FlowerStatus.Cancelled,
            _ => FlowerStatus.SpellUnavailable
        };
}
