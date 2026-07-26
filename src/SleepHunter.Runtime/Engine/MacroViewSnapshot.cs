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

public sealed record MacroViewSnapshot(
    long Revision,
    MacroLifecycle Lifecycle,
    MacroStopReason StopReason,
    SnapshotSequence? LatestSnapshotSequence,
    ClientPresence Presence,
    MacroTimestamp? LastTransitionAt,
    ClientActionId? PendingActionId,
    AutomationConfiguration Automation,
    SpellQueueState SpellQueue,
    PanelTransitionState? PanelTransition,
    PanelPreservationState? PanelPreservation,
    StaffSwitchState? StaffSwitch,
    SpellCooldownState SpellCooldowns,
    SpellCastState? SpellCast,
    SkillQueueState SkillQueue,
    SkillCooldownState SkillCooldowns,
    SkillUseState? SkillUse,
    DisarmState? Disarm,
    DialogState? Dialog,
    FlowerQueueState FlowerQueue,
    FlowerScheduleState FlowerSchedules,
    ClientRosterSequence? ClientRosterSequence,
    FlowerState? Flower,
    TargetRotationState SpellTargetRotations,
    TargetRotationState FlowerTargetRotations,
    ClientActionIssue? LastActionIssue)
{
    internal static MacroViewSnapshot FromState(MacroState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new MacroViewSnapshot(
            state.Revision,
            state.Lifecycle,
            state.StopReason,
            state.LatestSnapshot?.Sequence,
            state.LatestSnapshot?.Presence ?? ClientPresence.Unknown,
            state.LastTransitionAt,
            state.PendingAction?.Intent.ActionId,
            state.Automation,
            state.SpellQueue,
            state.PanelTransition,
            state.PanelPreservation,
            state.StaffSwitch,
            state.SpellCooldowns,
            state.SpellCast,
            state.SkillQueue,
            state.SkillCooldowns,
            state.SkillUse,
            state.Disarm,
            state.Dialog,
            state.FlowerQueue,
            state.FlowerSchedules,
            state.ClientRoster.Sequence,
            state.Flower,
            state.SpellTargetRotations,
            state.FlowerTargetRotations,
            state.LastActionIssue);
    }
}
