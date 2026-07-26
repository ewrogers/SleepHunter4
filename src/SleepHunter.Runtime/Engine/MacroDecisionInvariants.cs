using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Flowering;
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
            if (pendingAction.IsIssued)
            {
                throw new InvalidOperationException(
                    "New client action intents must await issuance feedback.");
            }

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

        if (decision.State.PendingAction is { } observedPendingAction)
        {
            var matchingIssue =
                decision.State.LastActionIssue?.ActionId ==
                observedPendingAction.Intent.ActionId
                    ? decision.State.LastActionIssue
                    : null;
            if (observedPendingAction.IsIssued !=
                (matchingIssue?.Status == ClientActionIssueStatus.Issued))
            {
                throw new InvalidOperationException(
                    "Pending client action issuance state must match its observed issue.");
            }
        }

        if (decision.State.LastActionIssue is { WasIssued: false } failedIssue &&
            previousState.LastActionIssue != decision.State.LastActionIssue)
        {
            if (decision.State.Lifecycle != MacroLifecycle.Paused ||
                decision.State.PendingAction is not null)
            {
                throw new InvalidOperationException(
                    "Failed client action issuance must pause and clear pending work.");
            }

            if (previousState.PendingAction?.Intent.ActionId !=
                failedIssue.ActionId)
            {
                throw new InvalidOperationException(
                    "Failed client action issuance must match the pending action.");
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

        if (decision.State.PanelPreservation is
            {
                Status: PanelPreservationStatus.Restoring
            } restoring &&
            (pendingSwitchIntent is null ||
             !pendingSwitchIntent.TargetPanel.IsEquivalentTo(
                 restoring.OriginalPanel)))
        {
            throw new InvalidOperationException(
                "Restoring preserved panel state requires its matching panel action.");
        }

        if (decision.State.Lifecycle != MacroLifecycle.Running &&
            decision.State.PanelPreservation is { IsActive: true })
        {
            throw new InvalidOperationException(
                "Only running macro state can preserve an active panel.");
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

        ClientActionIntent? pendingInventoryModeIntent =
            decision.State.PendingAction?.Intent switch
            {
                ExpandInventoryIntent expand => expand,
                CollapseInventoryIntent collapse => collapse,
                _ => null
            };
        var changingInventoryMode = decision.State.StaffSwitch is
        {
            Status: StaffSwitchStatus.ChangingInventoryMode
        };

        if ((pendingInventoryModeIntent is not null) !=
            changingInventoryMode)
        {
            throw new InvalidOperationException(
                "Pending inventory mode state must match its client action.");
        }

        if (pendingInventoryModeIntent is not null &&
            (decision.State.StaffSwitch!.ActionId !=
             pendingInventoryModeIntent.ActionId ||
             decision.State.StaffSwitch.Attempt != 1 ||
             decision.State.PendingAction!.Attempt != 1 ||
             decision.State.PendingAction.MaximumAttempts != 1 ||
             decision.State.StaffSwitch.CompletedEquipmentAttempts >=
             decision.State.StaffSwitch.MaximumAttempts ||
             decision.State.StaffSwitch.TargetInventoryExpanded !=
             (pendingInventoryModeIntent is ExpandInventoryIntent) ||
             decision.State.StaffSwitch.Selection?.Action !=
             StaffSelectionAction.Equip))
        {
            throw new InvalidOperationException(
                "Pending inventory mode metadata must match its client action.");
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

        if (pendingCastIntent is not null && !castingSpell)
        {
            throw new InvalidOperationException(
                "Pending spell cast state must match its client action.");
        }

        if (castingSpell &&
            (decision.State.SpellCast!.ActionId is null ||
             decision.State.SpellCast.CompletesAt is null))
        {
            throw new InvalidOperationException(
                "Casting spell state requires bounded completion metadata.");
        }

        if (pendingCastIntent is not null &&
            (decision.State.SpellCast!.ActionId !=
             pendingCastIntent.ActionId ||
             decision.State.SpellCast.CompletesAt !=
             decision.State.PendingAction!.Deadline ||
             decision.State.PendingAction.Attempt != 1 ||
             decision.State.PendingAction.MaximumAttempts != 1 ||
             !DoesCastIntentMatchPlan(
                 pendingCastIntent,
                 decision.State.SpellCast)))
        {
            throw new InvalidOperationException(
                "Pending spell cast metadata must match its client action.");
        }

        if (decision.State.SpellCast is
            {
                Status: SpellCastStatus.Casting,
                TargetStatus: not TargetLocationStatus.Resolved
            })
        {
            throw new InvalidOperationException(
                "Casting spell state requires a resolved target.");
        }

        if (decision.State.SpellCast is
            {
                Status: SpellCastStatus.TargetUnavailable
            } unavailableTarget &&
            (unavailableTarget.TargetStatus is
                null or TargetLocationStatus.Resolved ||
             unavailableTarget.ResolvedTarget is not null ||
             unavailableTarget.ActionId is not null ||
             unavailableTarget.CompletesAt is not null))
        {
            throw new InvalidOperationException(
                "Unavailable spell target state cannot contain issued action metadata.");
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
                      StaffSwitchStatus.ChangingInventoryMode or
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

        var activeFlowerSpellCast = decision.State.SpellCast is
        {
            Origin: SpellCastOrigin.Flower,
            Status: SpellCastStatus.WaitingForStaff or
                SpellCastStatus.WaitingForPanel or
                SpellCastStatus.Casting
        } activeFlowerCast
            ? activeFlowerCast
            : null;
        var flowerSpellEntry = decision.State.Flower?.SpellEntry;
        var hasFlowerAction =
            decision.State.Flower?.Action is not null &&
            flowerSpellEntry is not null;
        if (activeFlowerSpellCast is not null && !hasFlowerAction)
        {
            throw new InvalidOperationException(
                "Flower spell casting requires flower action state.");
        }

        var flowerSpellCast = hasFlowerAction &&
            decision.State.SpellCast is
            {
                Origin: SpellCastOrigin.Flower
            } flowerCast
                ? flowerCast
                : null;
        if (flowerSpellCast is not null &&
            flowerSpellEntry is not null &&
            !flowerSpellCast.Plan.Queue.Entries.Contains(flowerSpellEntry))
        {
            throw new InvalidOperationException(
                "Flower spell casting requires matching flower action state.");
        }

        var flowerStatusMatchesSpell = decision.State.Flower?.Status switch
        {
            FlowerStatus.WaitingForStaff =>
                flowerSpellCast?.Status == SpellCastStatus.WaitingForStaff,
            FlowerStatus.WaitingForPanel =>
                flowerSpellCast?.Status == SpellCastStatus.WaitingForPanel,
            FlowerStatus.TargetUnavailable =>
                flowerSpellCast is null ||
                flowerSpellCast.Status == SpellCastStatus.TargetUnavailable,
            FlowerStatus.IssueFailed =>
                flowerSpellCast is null ||
                flowerSpellCast.Status == SpellCastStatus.IssueFailed,
            FlowerStatus.Casting =>
                flowerSpellCast?.Status == SpellCastStatus.Casting,
            FlowerStatus.Succeeded =>
                flowerSpellCast is null ||
                flowerSpellCast?.Status == SpellCastStatus.Succeeded,
            FlowerStatus.StaffUnavailable =>
                flowerSpellCast is null ||
                flowerSpellCast?.Status == SpellCastStatus.StaffUnavailable,
            FlowerStatus.PanelUnavailable =>
                flowerSpellCast is null ||
                flowerSpellCast?.Status == SpellCastStatus.PanelUnavailable,
            FlowerStatus.Cancelled =>
                flowerSpellCast is null ||
                flowerSpellCast.Status == SpellCastStatus.Cancelled,
            _ => true
        };
        if (!flowerStatusMatchesSpell)
        {
            throw new InvalidOperationException(
                $"Flower action status '{decision.State.Flower?.Status}' " +
                "must match spell cast origin " +
                $"'{decision.State.SpellCast?.Origin}' and status " +
                $"'{decision.State.SpellCast?.Status}'.");
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
             decision.State.PendingAction.MaximumAttempts != 1 ||
             decision.State.Dialog.LastCancelSnapshotSequence !=
             decision.State.PendingAction.BaselineSnapshotSequence ||
             decision.State.Dialog.SnapshotRequiredAfter is not null))
        {
            throw new InvalidOperationException(
                "Pending dialog metadata must match its client action.");
        }

        if (decision.State.Dialog is
            {
                Status: DialogStatus.AwaitingObservation
            } awaitingDialog &&
            (awaitingDialog.ActionId is not null ||
             awaitingDialog.CompletesAt is not null ||
             awaitingDialog.LastCancelSnapshotSequence is null ||
             awaitingDialog.SnapshotRequiredAfter is null))
        {
            throw new InvalidOperationException(
                "A dialog awaiting observation requires a completed cancellation baseline.");
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
        SpellCastState state) =>
        state.Plan is
        {
            SelectedSpell: { } spell
        } &&
        string.Equals(
            spell.Name,
            intent.SpellName,
            StringComparison.OrdinalIgnoreCase) &&
        spell.Slot == intent.Slot &&
        spell.Panel == intent.Panel &&
        state.ResolvedTarget == intent.Target;

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
