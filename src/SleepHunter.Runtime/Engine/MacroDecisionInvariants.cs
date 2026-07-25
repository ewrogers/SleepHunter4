using SleepHunter.Runtime.Automation.Panels;
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
            decision.State.PendingAction?.Intent as SetEquippedWeaponIntent;
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

        if (decision.ScheduledEvents.Any(
                scheduledEvent => scheduledEvent.DueAt < currentTime))
        {
            throw new InvalidOperationException(
                "Scheduled events cannot be earlier than the current time.");
        }
    }

    private static bool DoesWeaponIntentMatchSelection(
        SetEquippedWeaponIntent intent,
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
}
