using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

internal static class MacroDecisionInvariants
{
    public static void EnsureValid(
        MacroState previousState,
        MacroDecision decision,
        MacroTimestamp currentTime)
    {
        if (decision.State.Revision < previousState.Revision)
        {
            throw new InvalidOperationException("State revisions cannot move backward.");
        }

        var stateChanged = decision.State.Revision != previousState.Revision;
        if (stateChanged != (decision.PublishedView is not null))
        {
            throw new InvalidOperationException(
                "Every state revision must publish exactly one matching view.");
        }

        if (decision.PublishedView is not null &&
            decision.PublishedView.Revision != decision.State.Revision)
        {
            throw new InvalidOperationException(
                "Published view revision must match the state revision.");
        }

        if (decision.Intent is ClientActionIntent clientActionIntent)
        {
            if (decision.State.Lifecycle != MacroLifecycle.Running)
            {
                throw new InvalidOperationException(
                    "Client action intents are only allowed while the macro is running.");
            }

            if (decision.State.PendingAction?.Intent.ActionId !=
                clientActionIntent.ActionId)
            {
                throw new InvalidOperationException(
                    "Client action intents require matching bounded pending action state.");
            }

            var pendingAction = decision.State.PendingAction!;
            var matchingDeadlines = decision.ScheduledEvents.Count(
                scheduledEvent =>
                    scheduledEvent.Input is ClientActionDeadlineElapsed deadline &&
                    deadline.ActionId == clientActionIntent.ActionId &&
                    scheduledEvent.DueAt == pendingAction.Deadline);

            if (matchingDeadlines != 1)
            {
                throw new InvalidOperationException(
                    "Client action intents require exactly one matching deadline event.");
            }
        }

        var pendingSwitchIntent =
            decision.State.PendingAction?.Intent as SwitchPanelIntent;
        var pendingPanelTransition = decision.State.PanelTransition is
        {
            Status: PanelTransitionStatus.Pending
        };

        if ((pendingSwitchIntent is not null) != pendingPanelTransition)
        {
            throw new InvalidOperationException(
                "Pending panel transition state must match its client action.");
        }

        if (pendingSwitchIntent is not null &&
            (decision.State.PanelTransition!.ActionId !=
             pendingSwitchIntent.ActionId ||
             decision.State.PanelTransition.TargetPanel !=
             pendingSwitchIntent.TargetPanel ||
             decision.State.PanelTransition.Attempt !=
             decision.State.PendingAction!.Attempt ||
             decision.State.PanelTransition.MaximumAttempts !=
             decision.State.PendingAction.MaximumAttempts))
        {
            throw new InvalidOperationException(
                "Pending panel transition metadata must match its client action.");
        }

        var pendingWeaponIntent =
            decision.State.PendingAction?.Intent as EquipWeaponIntent;
        var changingWeapon = decision.State.StaffSwitch is
        {
            Status: StaffSwitchStatus.ChangingWeapon
        };

        if ((pendingWeaponIntent is not null) != changingWeapon)
        {
            throw new InvalidOperationException(
                "Pending staff equipment state must match its client action.");
        }

        if (pendingWeaponIntent is not null &&
            (decision.State.StaffSwitch!.ActionId !=
             pendingWeaponIntent.ActionId ||
             decision.State.StaffSwitch.Attempt !=
             decision.State.PendingAction!.Attempt ||
             decision.State.StaffSwitch.MaximumAttempts !=
             decision.State.PendingAction.MaximumAttempts ||
             !DoesWeaponIntentMatchSelection(
                 pendingWeaponIntent,
                 decision.State.StaffSwitch.Selection)))
        {
            throw new InvalidOperationException(
                "Pending staff equipment metadata must match its client action.");
        }

        if (decision.State.StaffSwitch is
            {
                Status: StaffSwitchStatus.WaitingForInventory
            } &&
            pendingSwitchIntent?.TargetPanel != ClientPanel.Inventory)
        {
            throw new InvalidOperationException(
                "Staff equipment can wait only on a pending inventory panel action.");
        }

        var pendingCastIntent =
            decision.State.PendingAction?.Intent as CastSpellIntent;
        var castingSpell = decision.State.SpellCast is
        {
            Status: SpellCastStatus.Casting
        };

        if ((pendingCastIntent is not null) != castingSpell)
        {
            throw new InvalidOperationException(
                "Pending spell cast state must match its client action.");
        }

        if (pendingCastIntent is not null &&
            (decision.State.SpellCast!.ActionId != pendingCastIntent.ActionId ||
             decision.State.SpellCast.CompletesAt !=
             decision.State.PendingAction!.Deadline ||
             decision.State.PendingAction.Attempt != 1 ||
             decision.State.PendingAction.MaximumAttempts != 1 ||
             !DoesCastIntentMatchPlan(
                 pendingCastIntent,
                 decision.State.SpellCast.Plan)))
        {
            throw new InvalidOperationException(
                "Pending spell cast metadata must match its client action.");
        }

        if (decision.State.SpellCast is
            {
                Status: SpellCastStatus.WaitingForStaff
            } waitingForStaff &&
            (waitingForStaff.StaffSelection is not
            { } spellStaffSelection ||
             decision.State.StaffSwitch is not
             {
                 Status: StaffSwitchStatus.WaitingForInventory or
                      StaffSwitchStatus.ChangingWeapon,
                 Selection: { } staffSelection
             } ||
             staffSelection != spellStaffSelection))
        {
            throw new InvalidOperationException(
                "Spell casting can wait only on its matching staff action.");
        }

        if (decision.State.SpellCast is
            {
                Status: SpellCastStatus.WaitingForPanel,
                Plan.SelectedSpell: { } selectedSpell
            } &&
            (pendingSwitchIntent is null ||
             !pendingSwitchIntent.TargetPanel.IsEquivalentTo(
                 selectedSpell.Panel)))
        {
            throw new InvalidOperationException(
                "Spell casting can wait only on its pending spell panel action.");
        }

        var pendingDisarmIntent =
            decision.State.PendingAction?.Intent as DisarmIntent;
        var disarming = decision.State.Disarm is
        {
            Status: DisarmStatus.Disarming
        };

        if ((pendingDisarmIntent is not null) != disarming)
        {
            throw new InvalidOperationException(
                "Pending disarm state must match its client action.");
        }

        if (pendingDisarmIntent is not null &&
            (decision.State.Disarm!.ActionId != pendingDisarmIntent.ActionId ||
             decision.State.Disarm.Attempt !=
             decision.State.PendingAction!.Attempt ||
             decision.State.Disarm.MaximumAttempts !=
             decision.State.PendingAction.MaximumAttempts ||
             decision.State.SkillUse is not
             {
                 Status: SkillUseStatus.WaitingForDisarm
             }))
        {
            throw new InvalidOperationException(
                "Pending disarm metadata must match its skill action.");
        }

        var pendingSkillIntent =
            decision.State.PendingAction?.Intent as UseSkillIntent;
        var pendingAssailIntent =
            decision.State.PendingAction?.Intent as AssailIntent;
        var actingSkill = decision.State.SkillUse is
        {
            Status: SkillUseStatus.Using or SkillUseStatus.Assailing
        };

        if (((pendingSkillIntent is not null) ||
             (pendingAssailIntent is not null)) != actingSkill)
        {
            throw new InvalidOperationException(
                "Pending skill use state must match its client action.");
        }

        if ((pendingSkillIntent is not null ||
             pendingAssailIntent is not null) &&
            (decision.State.SkillUse!.ActionId !=
             decision.State.PendingAction!.Intent.ActionId ||
             decision.State.SkillUse.CompletesAt !=
             decision.State.PendingAction.Deadline ||
             decision.State.PendingAction.Attempt != 1 ||
             decision.State.PendingAction.MaximumAttempts != 1 ||
             !DoesSkillIntentMatchPlan(
                 decision.State.PendingAction.Intent,
                 decision.State.SkillUse.Plan)))
        {
            throw new InvalidOperationException(
                "Pending skill use metadata must match its client action.");
        }

        if (decision.State.SkillUse is
            {
                Status: SkillUseStatus.WaitingForPanel,
                Plan.SelectedSkill: { } waitingSkill
            } &&
            (pendingSwitchIntent is null ||
             !pendingSwitchIntent.TargetPanel.IsEquivalentTo(
                 waitingSkill.Panel)))
        {
            throw new InvalidOperationException(
                "Skill use can wait only on its pending skill panel action.");
        }

        if (decision.State.SkillUse is
            {
                Status: SkillUseStatus.WaitingForDisarm
            } &&
            pendingDisarmIntent is null)
        {
            throw new InvalidOperationException(
                "Skill use can wait only on its pending disarm action.");
        }

        var pendingDialogIntent =
            decision.State.PendingAction?.Intent as CancelDialogIntent;
        var closingDialog = decision.State.Dialog is
        {
            Status: DialogStatus.Closing
        };

        if ((pendingDialogIntent is not null) != closingDialog)
        {
            throw new InvalidOperationException(
                "Pending dialog state must match its client action.");
        }

        if (pendingDialogIntent is not null &&
            (decision.State.Dialog!.ActionId != pendingDialogIntent.ActionId ||
             decision.State.Dialog.CompletesAt !=
             decision.State.PendingAction!.Deadline ||
             decision.State.PendingAction.Attempt != 1 ||
             decision.State.PendingAction.MaximumAttempts != 1))
        {
            throw new InvalidOperationException(
                "Pending dialog metadata must match its client action.");
        }

        if (decision.State.Dialog is
            {
                Status: DialogStatus.Scheduled,
                DueAt: { } dialogDueAt
            } &&
            (previousState.Dialog is not
            {
                Status: DialogStatus.Scheduled,
                DueAt: { } previousDialogDueAt
            } ||
             previousDialogDueAt != dialogDueAt))
        {
            var matchingDialogEvents = decision.ScheduledEvents.Count(
                scheduledEvent =>
                    scheduledEvent.Input is DialogCloseDue closeDue &&
                    closeDue.DueAt == dialogDueAt &&
                    scheduledEvent.DueAt == dialogDueAt);

            if (matchingDialogEvents != 1)
            {
                throw new InvalidOperationException(
                    "Scheduled dialog state requires exactly one matching close event.");
            }
        }

        if (decision.ScheduledEvents.Any(
                scheduledEvent => scheduledEvent.DueAt < currentTime))
        {
            throw new InvalidOperationException(
                "Scheduled events cannot be earlier than the current time.");
        }
    }

    private static bool DoesWeaponIntentMatchSelection(
        EquipWeaponIntent intent,
        StaffSelection? selection)
    {
        if (selection is null)
        {
            return false;
        }

        return selection.Action switch
        {
            StaffSelectionAction.Equip =>
                string.Equals(
                    selection.Staff?.Name,
                    intent.StaffName,
                    StringComparison.OrdinalIgnoreCase) &&
                selection.InventorySlot == intent.InventorySlot,
            StaffSelectionAction.Unequip => intent.IsUnequip,
            _ => false
        };
    }

    private static bool DoesCastIntentMatchPlan(
        CastSpellIntent intent,
        SpellCastPlan plan) =>
        plan is
        {
            SelectedEntry: { } entry,
            SelectedSpell: { } spell
        } &&
        string.Equals(
            spell.Name,
            intent.SpellName,
            StringComparison.OrdinalIgnoreCase) &&
        spell.Slot == intent.Slot &&
        spell.Panel == intent.Panel &&
        entry.Target == intent.Target;

    private static bool DoesSkillIntentMatchPlan(
        ClientActionIntent intent,
        SkillPlan plan) =>
        plan.SelectedSkill is { } skill &&
        intent switch
        {
            UseSkillIntent useSkill =>
                plan.ActionKind == SkillActionKind.UseSkill &&
                string.Equals(
                    skill.Name,
                    useSkill.SkillName,
                    StringComparison.OrdinalIgnoreCase) &&
                skill.Slot == useSkill.Slot &&
                skill.Panel == useSkill.Panel,
            AssailIntent assail =>
                plan.ActionKind == SkillActionKind.Assail &&
                string.Equals(
                    skill.Name,
                    assail.SkillName,
                    StringComparison.OrdinalIgnoreCase),
            _ => false
        };
}
