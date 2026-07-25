using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine
{
    private static MacroDecision RequestStaffSwitch(
        MacroState currentState,
        RequestStaffSwitchCommand command,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not null ||
            currentState.LatestSnapshot is not
            {
                Presence: ClientPresence.InWorld
            } snapshot)
        {
            return Unchanged(currentState);
        }

        if (snapshot.Character is null ||
            snapshot.Inventory is null ||
            snapshot.Equipment is null)
        {
            return ChangeStaffSwitch(
                currentState,
                StaffSwitchState.SnapshotUnavailable());
        }

        var selection = StaffSelector.Select(
            new StaffSelectionRequest(
                command.BaseCastLines,
                snapshot.Character,
                snapshot.Inventory,
                snapshot.Equipment,
                command.Candidates));

        if (selection.Action == StaffSelectionAction.None)
        {
            return ChangeStaffSwitch(
                currentState,
                StaffSwitchState.NoChange(selection));
        }

        return BeginStaffSwitch(
            currentState,
            selection,
            command.Policy,
            currentTime);
    }

    private static MacroDecision BeginStaffSwitch(
        MacroState currentState,
        StaffSelection selection,
        StaffEquipmentPolicy policy,
        MacroTimestamp currentTime,
        SpellCastState? spellCast = null)
    {
        var snapshot = currentState.LatestSnapshot!;

        if (selection.Action == StaffSelectionAction.Equip &&
            !snapshot.ActivePanel.IsEquivalentTo(ClientPanel.Inventory))
        {
            var staffSwitch = StaffSwitchState.WaitingForInventory(
                selection,
                policy,
                completedAttempts: 0);

            return IssuePanelTransitionAttempt(
                currentState,
                ClientPanel.Inventory,
                policy.AttemptTimeout,
                attempt: 1,
                policy.MaximumAttempts,
                currentTime,
                staffSwitch,
                spellCast,
                spellCast?.Plan.Cooldowns);
        }

        return IssueStaffEquipmentAttempt(
            currentState,
            selection,
            policy.AttemptTimeout,
            attempt: 1,
            policy.MaximumAttempts,
            currentTime,
            spellCast: spellCast);
    }

    private static MacroDecision HandleStaffEquipmentDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        EquipWeaponIntent weaponIntent,
        MacroTimestamp currentTime)
    {
        if (currentState.StaffSwitch is not
            {
                Status: StaffSwitchStatus.ChangingWeapon,
                Selection: { } selection
            } staffSwitch ||
            staffSwitch.ActionId != weaponIntent.ActionId)
        {
            return Unchanged(currentState);
        }

        if (pendingAction.Attempt >= pendingAction.MaximumAttempts)
        {
            var spellCast = currentState.SpellCast is
            {
                Status: SpellCastStatus.WaitingForStaff
            } waitingForStaff
                ? waitingForStaff.StaffUnavailable()
                : currentState.SpellCast;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                staffSwitch: staffSwitch.TimedOut(),
                spellCast: spellCast);
        }

        if (currentState.LatestSnapshot is not { } snapshot ||
            !IsStaffSelectionStillValid(selection, snapshot))
        {
            var spellCast = currentState.SpellCast is
            {
                Status: SpellCastStatus.WaitingForStaff
            } waitingForStaff
                ? waitingForStaff.StaffUnavailable()
                : currentState.SpellCast;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                staffSwitch: staffSwitch.SelectionInvalidated(),
                spellCast: spellCast);
        }

        if (selection.Action == StaffSelectionAction.Equip &&
            !snapshot.ActivePanel.IsEquivalentTo(ClientPanel.Inventory))
        {
            var policy = new StaffEquipmentPolicy(
                staffSwitch.AttemptTimeout,
                staffSwitch.MaximumAttempts);
            var waiting = StaffSwitchState.WaitingForInventory(
                selection,
                policy,
                completedAttempts: pendingAction.Attempt);

            return IssuePanelTransitionAttempt(
                currentState,
                ClientPanel.Inventory,
                staffSwitch.AttemptTimeout,
                attempt: 1,
                staffSwitch.MaximumAttempts,
                currentTime,
                waiting,
                currentState.SpellCast,
                currentState.SpellCooldowns);
        }

        return IssueStaffEquipmentAttempt(
            currentState,
            selection,
            staffSwitch.AttemptTimeout,
            checked(pendingAction.Attempt + 1),
            staffSwitch.MaximumAttempts,
            currentTime,
            spellCast: currentState.SpellCast);
    }

    private static MacroDecision IssueStaffEquipmentAttempt(
        MacroState currentState,
        StaffSelection selection,
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        MacroTimestamp currentTime,
        ClientSnapshot? latestSnapshot = null,
        PanelTransitionState? panelTransition = null,
        SpellCastState? spellCast = null)
    {
        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = selection.Action switch
        {
            StaffSelectionAction.Equip => new EquipWeaponIntent(
                actionId,
                selection.Staff!.Name,
                selection.InventorySlot),
            StaffSelectionAction.Unequip => new EquipWeaponIntent(
                actionId,
                staffName: null,
                inventorySlot: null),
            _ => throw new InvalidOperationException(
                "Staff equipment attempts require an equipment action.")
        };
        var snapshot = latestSnapshot ?? currentState.LatestSnapshot;
        var deadline = currentTime.Add(attemptTimeout);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt,
            maximumAttempts,
            snapshot?.Sequence);
        var staffSwitch = StaffSwitchState.ChangingWeapon(
            selection,
            attemptTimeout,
            attempt,
            maximumAttempts,
            actionId);

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
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static bool CanConfirmStaffEquipment(
        PendingAction? pendingAction,
        ClientSnapshot snapshot)
    {
        if (pendingAction?.Intent is not EquipWeaponIntent weaponIntent ||
            snapshot.Equipment is not { } equipment)
        {
            return false;
        }

        var targetMatches = weaponIntent.IsUnequip
            ? equipment.WeaponName is null
            : string.Equals(
                equipment.WeaponName,
                weaponIntent.StaffName,
                StringComparison.OrdinalIgnoreCase);

        return targetMatches &&
               CanSnapshotConfirmAction(pendingAction, snapshot);
    }

    private static bool IsStaffSelectionStillValid(
        StaffSelection selection,
        ClientSnapshot snapshot)
    {
        if (snapshot.Equipment is null)
        {
            return false;
        }

        if (selection.Action == StaffSelectionAction.Unequip)
        {
            return true;
        }

        if (selection is not
            {
                Action: StaffSelectionAction.Equip,
                Staff: { } staff,
                InventorySlot: { } inventorySlot
            } ||
            snapshot.Character is not { } character ||
            snapshot.Inventory is not { } inventory ||
            !staff.IsEligibleFor(character))
        {
            return false;
        }

        var selectedItem = inventory.Items.FirstOrDefault(
            item => item.Slot == inventorySlot);
        return selectedItem is not null &&
               string.Equals(
                   selectedItem.Name,
                   staff.Name,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static StaffSwitchState? CancelPendingStaffSwitch(
        MacroState currentState) =>
        currentState.StaffSwitch is
        {
            Status: StaffSwitchStatus.WaitingForInventory or
                StaffSwitchStatus.ChangingWeapon
        } staffSwitch
            ? staffSwitch.Cancelled()
            : currentState.StaffSwitch;

    private static MacroDecision ChangeStaffSwitch(
        MacroState currentState,
        StaffSwitchState staffSwitch)
    {
        if (currentState.StaffSwitch == staffSwitch)
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            currentState.PendingAction,
            staffSwitch: staffSwitch);
    }
}
