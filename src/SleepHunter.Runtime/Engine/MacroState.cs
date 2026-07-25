using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
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
        StaffSwitchState? staffSwitch = null)
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

    internal long NextClientActionId { get; }
}
