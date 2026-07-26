using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillUseState
{
    private SkillUseState(
        SkillPlan plan,
        SkillExecutionPolicy policy,
        SkillUseStatus status,
        ClientActionId? actionId,
        MacroTimestamp? completesAt,
        MacroTimestamp? snapshotRequiredAfter)
    {
        Plan = plan;
        Policy = policy;
        Status = status;
        ActionId = actionId;
        CompletesAt = completesAt;
        SnapshotRequiredAfter = snapshotRequiredAfter;
    }

    public SkillPlan Plan { get; private init; }

    public SkillExecutionPolicy Policy { get; }

    public SkillUseStatus Status { get; private init; }

    public ClientActionId? ActionId { get; private init; }

    public MacroTimestamp? CompletesAt { get; private init; }

    public MacroTimestamp? SnapshotRequiredAfter { get; private init; }

    internal static SkillUseState FromPlan(
        SkillPlan plan,
        SkillExecutionPolicy policy,
        MacroTimestamp? snapshotRequiredAfter = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policy);

        return new SkillUseState(
            plan,
            policy,
            ToStatus(plan),
            actionId: null,
            completesAt: null,
            snapshotRequiredAfter);
    }

    internal SkillUseState WaitingForDisarm(SkillPlan? plan = null) =>
        this with
        {
            Plan = plan ?? Plan,
            Status = SkillUseStatus.WaitingForDisarm,
            ActionId = null,
            CompletesAt = null
        };

    internal SkillUseState WaitingForPanel(SkillPlan? plan = null) =>
        this with
        {
            Plan = plan ?? Plan,
            Status = SkillUseStatus.WaitingForPanel,
            ActionId = null,
            CompletesAt = null
        };

    internal SkillUseState Acting(
        SkillPlan plan,
        ClientActionId actionId,
        MacroTimestamp completesAt) =>
        this with
        {
            Plan = plan,
            Status = plan.ActionKind == SkillActionKind.Assail
                ? SkillUseStatus.Assailing
                : SkillUseStatus.Using,
            ActionId = actionId,
            CompletesAt = completesAt,
            SnapshotRequiredAfter = completesAt
        };

    internal SkillUseState WithPlan(SkillPlan plan) =>
        this with { Plan = plan };

    internal SkillUseState Replanned(SkillPlan plan) =>
        FromPlan(plan, Policy, SnapshotRequiredAfter);

    internal SkillUseState SelectionInvalidated(SkillPlan plan) =>
        FromPlan(plan, Policy, SnapshotRequiredAfter) with
        {
            Status = SkillUseStatus.SelectionInvalidated
        };

    internal SkillUseState Succeeded() =>
        this with { Status = SkillUseStatus.Succeeded };

    internal SkillUseState DisarmUnavailable() =>
        this with { Status = SkillUseStatus.DisarmUnavailable };

    internal SkillUseState SnapshotUnavailable() =>
        this with { Status = SkillUseStatus.SnapshotUnavailable };

    internal SkillUseState PanelUnavailable() =>
        this with { Status = SkillUseStatus.PanelUnavailable };

    internal SkillUseState IssueFailed() =>
        this with { Status = SkillUseStatus.IssueFailed };

    internal SkillUseState Cancelled() =>
        this with { Status = SkillUseStatus.Cancelled };

    private static SkillUseStatus ToStatus(SkillPlan plan) =>
        plan.Status switch
        {
            SkillPlanStatus.QueueEmpty => SkillUseStatus.QueueEmpty,
            SkillPlanStatus.SnapshotUnavailable =>
                SkillUseStatus.SnapshotUnavailable,
            SkillPlanStatus.Unavailable => SkillUseStatus.Unavailable,
            SkillPlanStatus.Waiting => GetWaitingStatus(plan),
            SkillPlanStatus.Ready => SkillUseStatus.WaitingForPanel,
            _ => throw new ArgumentOutOfRangeException(
                nameof(plan),
                plan.Status,
                "Skill plan status is not supported.")
        };

    private static SkillUseStatus GetWaitingStatus(SkillPlan plan)
    {
        var waitingForHealth = plan.Readiness.Any(
            readiness =>
                readiness.Status == SkillReadinessStatus.WaitingForHealth);
        var waitingForMana = plan.Readiness.Any(
            readiness =>
                readiness.Status == SkillReadinessStatus.WaitingForMana);
        var coolingDown = plan.Readiness.Any(
            readiness =>
                readiness.Status == SkillReadinessStatus.CoolingDown);

        return (waitingForHealth, waitingForMana, coolingDown) switch
        {
            (true, false, false) => SkillUseStatus.WaitingForHealth,
            (false, true, false) => SkillUseStatus.WaitingForMana,
            (false, false, true) => SkillUseStatus.CoolingDown,
            _ => SkillUseStatus.Waiting
        };
    }
}
