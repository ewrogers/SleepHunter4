using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed record MacroState
{
    public static MacroState Initial { get; } = new(
        revision: 0,
        MacroLifecycle.Stopped,
        MacroStopReason.None,
        latestSnapshot: null,
        lastTransitionAt: null,
        pendingAction: null);

    internal MacroState(
        long revision,
        MacroLifecycle lifecycle,
        MacroStopReason stopReason,
        ClientSnapshot? latestSnapshot,
        MacroTimestamp? lastTransitionAt,
        PendingAction? pendingAction,
        SpellQueueState? spellQueue = null,
        PanelTransitionState? panelTransition = null,
        long nextClientActionId = 1,
        StaffSwitchState? staffSwitch = null,
        SpellCooldownState? spellCooldowns = null,
        SpellCastState? spellCast = null,
        SkillQueueState? skillQueue = null,
        SkillCooldownState? skillCooldowns = null,
        SkillUseState? skillUse = null,
        DisarmState? disarm = null,
        DialogState? dialog = null,
        FlowerQueueState? flowerQueue = null,
        FlowerScheduleState? flowerSchedules = null,
        ClientRosterSnapshot? clientRoster = null,
        FlowerState? flower = null,
        TargetRotationState? spellTargetRotations = null,
        TargetRotationState? flowerTargetRotations = null,
        ClientActionIssue? lastActionIssue = null,
        AutomationConfiguration? automation = null,
        PanelPreservationState? panelPreservation = null)
    {
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revision),
                revision,
                "State revisions cannot be negative.");
        }

        if (lifecycle != MacroLifecycle.Stopped &&
            stopReason != MacroStopReason.None)
        {
            throw new ArgumentException(
                "Only stopped macro state can have a stop reason.",
                nameof(stopReason));
        }

        if (lifecycle != MacroLifecycle.Running && pendingAction is not null)
        {
            throw new ArgumentException(
                "Only running macro state can contain a pending action.",
                nameof(pendingAction));
        }

        if (lifecycle != MacroLifecycle.Running &&
            panelPreservation is { IsActive: true })
        {
            throw new ArgumentException(
                "Only running macro state can preserve an active panel.",
                nameof(panelPreservation));
        }

        if (nextClientActionId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextClientActionId),
                nextClientActionId,
                "The next client action identifier must be positive.");
        }

        Revision = revision;
        Lifecycle = lifecycle;
        StopReason = stopReason;
        LatestSnapshot = latestSnapshot;
        LastTransitionAt = lastTransitionAt;
        PendingAction = pendingAction;
        SpellQueue = spellQueue ?? SpellQueueState.Empty;
        PanelTransition = panelTransition;
        NextClientActionId = nextClientActionId;
        StaffSwitch = staffSwitch;
        SpellCooldowns = spellCooldowns ?? SpellCooldownState.Empty;
        SpellCast = spellCast;
        SkillQueue = skillQueue ?? SkillQueueState.Empty;
        SkillCooldowns = skillCooldowns ?? SkillCooldownState.Empty;
        SkillUse = skillUse;
        Disarm = disarm;
        Dialog = dialog;
        FlowerQueue = flowerQueue ?? FlowerQueueState.Empty;
        FlowerSchedules = flowerSchedules ?? FlowerScheduleState.Empty;
        ClientRoster = clientRoster ?? ClientRosterSnapshot.Empty;
        Flower = flower;
        SpellTargetRotations =
            spellTargetRotations ?? TargetRotationState.Empty;
        FlowerTargetRotations =
            flowerTargetRotations ?? TargetRotationState.Empty;
        LastActionIssue = lastActionIssue;
        Automation = automation ?? AutomationConfiguration.Disabled;
        PanelPreservation = panelPreservation;
    }

    public long Revision { get; }

    public MacroLifecycle Lifecycle { get; }

    public MacroStopReason StopReason { get; }

    public ClientSnapshot? LatestSnapshot { get; }

    public MacroTimestamp? LastTransitionAt { get; }

    public PendingAction? PendingAction { get; }

    public SpellQueueState SpellQueue { get; }

    public PanelTransitionState? PanelTransition { get; }

    public StaffSwitchState? StaffSwitch { get; }

    public SpellCooldownState SpellCooldowns { get; }

    public SpellCastState? SpellCast { get; }

    public SkillQueueState SkillQueue { get; }

    public SkillCooldownState SkillCooldowns { get; }

    public SkillUseState? SkillUse { get; }

    public DisarmState? Disarm { get; }

    public DialogState? Dialog { get; }

    public FlowerQueueState FlowerQueue { get; }

    public FlowerScheduleState FlowerSchedules { get; }

    public ClientRosterSnapshot ClientRoster { get; }

    public FlowerState? Flower { get; }

    public TargetRotationState SpellTargetRotations { get; }

    public TargetRotationState FlowerTargetRotations { get; }

    public ClientActionIssue? LastActionIssue { get; }

    public AutomationConfiguration Automation { get; }

    public PanelPreservationState? PanelPreservation { get; }

    internal long NextClientActionId { get; }

    internal bool HasSameContent(MacroState other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return WithRevision(0) == other.WithRevision(0);
    }

    internal MacroState WithRevision(long revision) =>
        new(
            revision,
            Lifecycle,
            StopReason,
            LatestSnapshot,
            LastTransitionAt,
            PendingAction,
            SpellQueue,
            PanelTransition,
            NextClientActionId,
            StaffSwitch,
            SpellCooldowns,
            SpellCast,
            SkillQueue,
            SkillCooldowns,
            SkillUse,
            Disarm,
            Dialog,
            FlowerQueue,
            FlowerSchedules,
            ClientRoster,
            Flower,
            SpellTargetRotations,
            FlowerTargetRotations,
            LastActionIssue,
            Automation,
            PanelPreservation);

    internal MacroState WithPanelPreservation(
        PanelPreservationState panelPreservation)
    {
        ArgumentNullException.ThrowIfNull(panelPreservation);

        return new MacroState(
            Revision,
            Lifecycle,
            StopReason,
            LatestSnapshot,
            LastTransitionAt,
            PendingAction,
            SpellQueue,
            PanelTransition,
            NextClientActionId,
            StaffSwitch,
            SpellCooldowns,
            SpellCast,
            SkillQueue,
            SkillCooldowns,
            SkillUse,
            Disarm,
            Dialog,
            FlowerQueue,
            FlowerSchedules,
            ClientRoster,
            Flower,
            SpellTargetRotations,
            FlowerTargetRotations,
            LastActionIssue,
            Automation,
            panelPreservation);
    }
}
