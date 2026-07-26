using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine
{
    private static MacroDecision IssueInterfaceExpansion(
        MacroState currentState,
        TimeSpan attemptTimeout,
        MacroTimestamp currentTime,
        ClientSnapshot snapshot,
        PanelTransitionState? panelTransition,
        StaffSwitchState? staffSwitch,
        SpellCastState? spellCast,
        SkillUseState? skillUse,
        DisarmState? disarm,
        FlowerState? flower)
    {
        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = new ExpandInterfaceIntent(actionId);
        var deadline = currentTime.Add(attemptTimeout);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            snapshot.Sequence);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCast: spellCast,
            skillUse: skillUse,
            disarm: disarm,
            flowerSchedules: flower?.Plan.Schedules,
            flower: flower,
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static MacroDecision IssueInterfaceExpansionForStaff(
        MacroState currentState,
        StaffSelection selection,
        TimeSpan attemptTimeout,
        int completedEquipmentAttempts,
        int maximumEquipmentAttempts,
        MacroTimestamp currentTime,
        ClientSnapshot snapshot,
        PanelTransitionState? panelTransition,
        SpellCastState? spellCast,
        FlowerState? flower)
    {
        var actionId = new ClientActionId(currentState.NextClientActionId);
        var staffSwitch = StaffSwitchState.ExpandingInterface(
            selection,
            attemptTimeout,
            completedEquipmentAttempts,
            maximumEquipmentAttempts,
            actionId);
        var intent = new ExpandInterfaceIntent(actionId);
        var deadline = currentTime.Add(attemptTimeout);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            snapshot.Sequence);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCast: spellCast,
            flowerSchedules: flower?.Plan.Schedules,
            flower: flower,
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static bool CanConfirmInterfaceExpansion(
        PendingAction? pendingAction,
        ClientSnapshot snapshot) =>
        pendingAction?.Intent is ExpandInterfaceIntent &&
        snapshot.IsPanelExpanded &&
        CanSnapshotConfirmAction(pendingAction, snapshot);

    private static MacroDecision HandleInterfaceExpansionDeadline(
        MacroState currentState,
        PendingAction pendingAction)
    {
        if (currentState.StaffSwitch is
            {
                Status: StaffSwitchStatus.ExpandingInterface
            } staffSwitch)
        {
            var staffSpellCast = currentState.SpellCast is
            {
                Status: SpellCastStatus.WaitingForStaff
            } waitingForStaff
                ? waitingForStaff.StaffUnavailable()
                : currentState.SpellCast;
            var flower = staffSpellCast?.Origin == SpellCastOrigin.Flower
                ? currentState.Flower?.StaffUnavailable()
                : currentState.Flower;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                staffSwitch: staffSwitch.TimedOut(),
                spellCast: staffSpellCast,
                flower: flower);
        }

        if (currentState.SpellCast is
            {
                Status: SpellCastStatus.WaitingForPanel
            } spellCast)
        {
            var unavailable = spellCast.PanelUnavailable();
            var flower = spellCast.Origin == SpellCastOrigin.Flower
                ? currentState.Flower?.PanelUnavailable()
                : currentState.Flower;
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                spellCast: unavailable,
                flower: flower);
        }

        if (currentState.SkillUse is
            {
                Status: SkillUseStatus.WaitingForPanel
            } skillUse)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                skillUse: skillUse.PanelUnavailable());
        }

        return Unchanged(currentState);
    }
}
