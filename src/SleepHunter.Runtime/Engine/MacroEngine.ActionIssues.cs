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
    private const int MaximumConsecutiveRecoverableActionFailures = 3;

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

        var feedbackDeadline =
            pendingAction.FeedbackDeadline ?? pendingAction.Deadline;
        if (issue.WasIssued && observedAt > feedbackDeadline)
        {
            issue = new ClientActionIssue(
                issue.ActionId,
                ClientActionIssueStatus.TimedOut,
                $"{DescribeClientAction(pendingAction.Intent)} " +
                "confirmation arrived after the recovery window.");
        }

        if (issue.WasIssued)
        {
            var nextPendingAction =
                pendingAction.Intent is
                    CastSpellIntent or
                    CancelSpellIntent
                    ? null
                    : pendingAction.MarkIssued(observedAt);
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                nextPendingAction,
                lastActionIssue: issue,
                pauseReason: MacroPauseReason.None,
                recoverableActionFailureCount: 0);
        }

        // Rejected and Failed guarantee that no intended input was posted.
        // Other outcomes may have reached the client and cannot be replayed safely.
        var isRecoverable = issue.Status is
            ClientActionIssueStatus.Rejected or
            ClientActionIssueStatus.Failed;
        var recoverableFailureCount = isRecoverable
            ? checked(currentState.RecoverableActionFailureCount + 1)
            : currentState.RecoverableActionFailureCount;
        var shouldRecover = isRecoverable &&
            recoverableFailureCount <
                MaximumConsecutiveRecoverableActionFailures;

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

            case ExpandInterfaceIntent:
                if (staffSwitch is
                    {
                        Status: StaffSwitchStatus.ExpandingInterface
                    })
                {
                    staffSwitch = staffSwitch.IssueFailed();
                }

                if (spellCast is
                    {
                        Status: SpellCastStatus.WaitingForPanel
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

            case CastSpellIntent:
                if (spellCast is
                    {
                        Status: SpellCastStatus.Casting
                    })
                {
                    spellCast = spellCast.IssueFailed(isRecoverable);
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
                    skillUse = skillUse.IssueFailed(isRecoverable);
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
                    ? shouldRecover
                        ? preservation.Retrying()
                        : preservation.IssueFailed()
                    : preservation.Cancelled();
        }

        return Changed(
            currentState,
            shouldRecover
                ? MacroLifecycle.Running
                : MacroLifecycle.Paused,
            currentState.StopReason,
            currentState.LatestSnapshot,
            shouldRecover
                ? currentState.LastTransitionAt
                : observedAt,
            pendingAction: null,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCast: spellCast,
            skillUse: skillUse,
            disarm: disarm,
            dialog: dialog,
            flower: flower,
            panelPreservation: panelPreservation,
            lastActionIssue: issue,
            pauseReason: shouldRecover
                ? MacroPauseReason.None
                : MacroPauseReason.ClientActionFailed,
            recoverableActionFailureCount: recoverableFailureCount);
    }

    private static string DescribeClientAction(ClientActionIntent intent) =>
        intent switch
        {
            UseSkillIntent useSkill =>
                $"Skill '{useSkill.SkillName}' input",
            AssailIntent assail =>
                $"Assail '{assail.SkillName}' input",
            CastSpellIntent castSpell =>
                $"Spell '{castSpell.SpellName}' input",
            CancelSpellIntent => "Spell cancellation input",
            SwitchPanelIntent => "Panel switch input",
            ExpandInterfaceIntent => "Interface expansion input",
            ExpandInventoryIntent => "Inventory expansion input",
            CollapseInventoryIntent => "Inventory collapse input",
            EquipWeaponIntent equipWeapon when equipWeapon.IsUnequip =>
                "Weapon removal input",
            EquipWeaponIntent equipWeapon =>
                $"Staff '{equipWeapon.StaffName}' input",
            DisarmIntent => "Disarm input",
            CancelDialogIntent => "Dialog cancellation input",
            _ => "Client input"
        };
}
