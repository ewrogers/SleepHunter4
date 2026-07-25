using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine
{
    private static MacroDecision HandleClientActionIssue(
        MacroState currentState,
        ClientActionIssue issue,
        MacroTimestamp observedAt)
    {
        var pendingAction = currentState.PendingAction;
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            pendingAction?.Intent.ActionId != issue.ActionId ||
            currentState.LastActionIssue?.ActionId == issue.ActionId)
        {
            return Unchanged(currentState);
        }

        if (issue.WasIssued && observedAt >= pendingAction.Deadline)
        {
            issue = new ClientActionIssue(
                issue.ActionId,
                ClientActionIssueStatus.TimedOut);
        }

        if (issue.WasIssued)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction.MarkIssued(observedAt),
                lastActionIssue: issue);
        }

        var panelTransition = currentState.PanelTransition;
        var staffSwitch = currentState.StaffSwitch;
        var spellCast = currentState.SpellCast;
        var skillUse = currentState.SkillUse;
        var disarm = currentState.Disarm;
        var dialog = currentState.Dialog;
        var panelPreservation = currentState.PanelPreservation;

        switch (pendingAction.Intent)
        {
            case SwitchPanelIntent:
                if (panelTransition is
                    {
                        Status: PanelTransitionStatus.Pending
                    })
                {
                    panelTransition = panelTransition.IssueFailed();
                }

                if (staffSwitch is
                    {
                        Status: StaffSwitchStatus.WaitingForInventory
                    })
                {
                    staffSwitch = staffSwitch.IssueFailed();
                }

                if (spellCast is
                    {
                        Status: SpellCastStatus.WaitingForPanel or
                            SpellCastStatus.WaitingForStaff
                    })
                {
                    spellCast = spellCast.IssueFailed();
                }

                if (skillUse is
                    {
                        Status: SkillUseStatus.WaitingForPanel
                    })
                {
                    skillUse = skillUse.IssueFailed();
                }

                break;

            case ExpandInventoryIntent:
            case CollapseInventoryIntent:
            case EquipWeaponIntent:
                if (staffSwitch is
                    {
                        Status: StaffSwitchStatus.ChangingInventoryMode or
                            StaffSwitchStatus.ChangingWeapon
                    })
                {
                    staffSwitch = staffSwitch.IssueFailed();
                }

                if (spellCast is
                    {
                        Status: SpellCastStatus.WaitingForStaff
                    })
                {
                    spellCast = spellCast.IssueFailed();
                }

                break;

            case CastSpellIntent:
                if (spellCast is
                    {
                        Status: SpellCastStatus.Casting
                    })
                {
                    spellCast = spellCast.IssueFailed();
                }

                break;

            case DisarmIntent:
                if (disarm is { Status: DisarmStatus.Disarming })
                {
                    disarm = disarm.IssueFailed();
                }

                if (skillUse is
                    {
                        Status: SkillUseStatus.WaitingForDisarm
                    })
                {
                    skillUse = skillUse.IssueFailed();
                }

                break;

            case UseSkillIntent:
            case AssailIntent:
                if (skillUse is
                    {
                        Status: SkillUseStatus.Using or
                            SkillUseStatus.Assailing
                    })
                {
                    skillUse = skillUse.IssueFailed();
                }

                break;

            case CancelDialogIntent:
                if (dialog is { Status: DialogStatus.Closing })
                {
                    dialog = dialog.IssueFailed();
                }

                break;
        }

        var flower = spellCast?.Origin == SpellCastOrigin.Flower
            ? currentState.Flower?.WithSpellCast(spellCast)
            : currentState.Flower;
        if (panelPreservation is { IsActive: true } preservation)
        {
            panelPreservation =
                preservation.Status == PanelPreservationStatus.Restoring &&
                pendingAction.Intent is SwitchPanelIntent
                    ? preservation.IssueFailed()
                    : preservation.Cancelled();
        }

        return Changed(
            currentState,
            MacroLifecycle.Paused,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            pendingAction: null,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCast: spellCast,
            skillUse: skillUse,
            disarm: disarm,
            dialog: dialog,
            flower: flower,
            panelPreservation: panelPreservation,
            lastActionIssue: issue);
    }
}
