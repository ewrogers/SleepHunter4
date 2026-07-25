using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellCastState
{
    private SpellCastState(
        SpellCastPlan plan,
        SpellExecutionPolicy policy,
        SpellCastStatus status,
        StaffSelection? staffSelection,
        int? castLines,
        TimeSpan? castDuration,
        ClientActionId? actionId,
        MacroTimestamp? completesAt,
        MacroTimestamp? snapshotRequiredAfter)
    {
        Plan = plan;
        Policy = policy;
        Status = status;
        StaffSelection = staffSelection;
        CastLines = castLines;
        CastDuration = castDuration;
        ActionId = actionId;
        CompletesAt = completesAt;
        SnapshotRequiredAfter = snapshotRequiredAfter;
    }

    public SpellCastPlan Plan { get; private init; }

    public SpellExecutionPolicy Policy { get; }

    public SpellCastStatus Status { get; private init; }

    public StaffSelection? StaffSelection { get; private init; }

    public int? CastLines { get; private init; }

    public TimeSpan? CastDuration { get; private init; }

    public ClientActionId? ActionId { get; private init; }

    public MacroTimestamp? CompletesAt { get; private init; }

    public MacroTimestamp? SnapshotRequiredAfter { get; private init; }

    internal static SpellCastState FromPlan(
        SpellCastPlan plan,
        SpellExecutionPolicy policy,
        MacroTimestamp? snapshotRequiredAfter = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);

        return new SpellCastState(
            plan,
            policy,
            ToStatus(plan),
            staffSelection: null,
            plan.SelectedSpell?.CastLines,
            plan.CastDuration,
            actionId: null,
            completesAt: null,
            snapshotRequiredAfter);
    }

    internal SpellCastState WaitingForStaff(
        StaffSelection staffSelection,
        SpellCastPlan? plan = null)
    {
        ArgumentNullException.ThrowIfNull(staffSelection);

        var castLines = staffSelection.CastLines;
        return this with
        {
            Plan = plan ?? Plan,
            Status = SpellCastStatus.WaitingForStaff,
            StaffSelection = staffSelection,
            CastLines = castLines,
            CastDuration = Policy.Cast.Timing.CalculateDuration(castLines),
            ActionId = null,
            CompletesAt = null
        };
    }

    internal SpellCastState WithStaffSelection(
        StaffSelection staffSelection)
    {
        ArgumentNullException.ThrowIfNull(staffSelection);

        var castLines = staffSelection.CastLines;
        return this with
        {
            StaffSelection = staffSelection,
            CastLines = castLines,
            CastDuration = Policy.Cast.Timing.CalculateDuration(castLines)
        };
    }

    internal SpellCastState WithPlan(SpellCastPlan plan) =>
        this with { Plan = plan };

    internal SpellCastState WaitingForPanel(SpellCastPlan? plan = null) =>
        this with
        {
            Plan = plan ?? Plan,
            Status = SpellCastStatus.WaitingForPanel,
            ActionId = null,
            CompletesAt = null
        };

    internal SpellCastState Casting(
        SpellCastPlan plan,
        ClientActionId actionId,
        MacroTimestamp completesAt) =>
        this with
        {
            Plan = plan,
            Status = SpellCastStatus.Casting,
            ActionId = actionId,
            CompletesAt = completesAt,
            SnapshotRequiredAfter = completesAt
        };

    internal SpellCastState Replanned(SpellCastPlan plan) =>
        FromPlan(plan, Policy, SnapshotRequiredAfter);

    internal SpellCastState Succeeded() =>
        this with { Status = SpellCastStatus.Succeeded };

    internal SpellCastState SelectionInvalidated(SpellCastPlan plan) =>
        FromPlan(plan, Policy, SnapshotRequiredAfter) with
        {
            Status = SpellCastStatus.SelectionInvalidated,
            ActionId = null
        };

    internal SpellCastState StaffUnavailable() =>
        this with { Status = SpellCastStatus.StaffUnavailable };

    internal SpellCastState SnapshotUnavailable() =>
        this with { Status = SpellCastStatus.SnapshotUnavailable };

    internal SpellCastState PanelUnavailable() =>
        this with { Status = SpellCastStatus.PanelUnavailable };

    internal SpellCastState Cancelled() =>
        this with { Status = SpellCastStatus.Cancelled };

    private static SpellCastStatus ToStatus(SpellCastPlan plan) =>
        plan.Status switch
        {
            SpellPlanStatus.QueueEmpty => SpellCastStatus.QueueEmpty,
            SpellPlanStatus.SnapshotUnavailable =>
                SpellCastStatus.SnapshotUnavailable,
            SpellPlanStatus.Complete => SpellCastStatus.Complete,
            SpellPlanStatus.Unavailable => SpellCastStatus.Unavailable,
            SpellPlanStatus.Waiting => GetWaitingStatus(plan),
            SpellPlanStatus.Ready => SpellCastStatus.WaitingForPanel,
            _ => throw new ArgumentOutOfRangeException(
                nameof(plan),
                plan.Status,
                "Spell plan status is not supported.")
        };

    private static SpellCastStatus GetWaitingStatus(SpellCastPlan plan)
    {
        var waitingForMana = plan.Readiness.Any(
            readiness =>
                readiness.Status == SpellReadinessStatus.WaitingForMana);
        var coolingDown = plan.Readiness.Any(
            readiness =>
                readiness.Status == SpellReadinessStatus.CoolingDown);

        return (waitingForMana, coolingDown) switch
        {
            (true, false) => SpellCastStatus.WaitingForMana,
            (false, true) => SpellCastStatus.CoolingDown,
            _ => SpellCastStatus.Waiting
        };
    }
}
