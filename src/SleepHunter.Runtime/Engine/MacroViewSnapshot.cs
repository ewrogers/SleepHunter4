using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
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
    SpellQueueState SpellQueue,
    PanelTransitionState? PanelTransition,
    StaffSwitchState? StaffSwitch,
    SpellCooldownState SpellCooldowns,
    SpellCastState? SpellCast)
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
            state.SpellQueue,
            state.PanelTransition,
            state.StaffSwitch,
            state.SpellCooldowns,
            state.SpellCast);
    }
}
