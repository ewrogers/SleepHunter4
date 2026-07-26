using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Flowering;
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
        SpellCastState? spellCast = null,
        FlowerState? flower = null)
    {
        return ContinueStaffSwitch(
            currentState,
            selection,
            policy.AttemptTimeout,
            policy.MaximumAttempts,
            completedEquipmentAttempts: 0,
            currentTime,
            spellCast: spellCast,
            flower: flower);
    }

    private static MacroDecision ContinueStaffSwitch(
        MacroState currentState,
        StaffSelection selection,
        TimeSpan attemptTimeout,
        int maximumAttempts,
        int completedEquipmentAttempts,
        MacroTimestamp currentTime,
        ClientSnapshot? latestSnapshot = null,
        PanelTransitionState? panelTransition = null,
        SpellCastState? spellCast = null,
        FlowerState? flower = null)
    {
        var snapshot = latestSnapshot ?? currentState.LatestSnapshot!;
        var policy = new StaffEquipmentPolicy(
            attemptTimeout,
            maximumAttempts);

        if (selection.Action == StaffSelectionAction.Equip &&
            !snapshot.ActivePanel.IsEquivalentTo(ClientPanel.Inventory))
        {
            var staffSwitch = StaffSwitchState.WaitingForInventory(
                selection,
                policy,
                completedEquipmentAttempts);

            return IssuePanelTransitionAttempt(
                currentState,
                ClientPanel.Inventory,
                attemptTimeout,
                attempt: 1,
                maximumAttempts,
                currentTime,
                staffSwitch,
                spellCast,
                spellCast?.Plan.Cooldowns,
                flower: flower);
        }

        if (selection is
            {
                Action: StaffSelectionAction.Equip,
                InventorySlot: { } inventorySlot
            })
        {
            if (snapshot.IsMinimizedMode &&
                !snapshot.IsPanelExpanded &&
                !ClientPanel.Inventory.IsSlotVisibleInMinimizedMode(
                    inventorySlot))
            {
                return IssueInterfaceExpansionForStaff(
                    currentState,
                    selection,
                    attemptTimeout,
                    completedEquipmentAttempts,
                    maximumAttempts,
                    currentTime,
                    snapshot,
                    panelTransition,
                    spellCast,
                    flower);
            }

            var targetInventoryExpanded =
                inventorySlot > InventoryItemSnapshot.MaximumCollapsedSlot;
            if (!snapshot.IsMinimizedMode &&
                snapshot.IsInventoryExpanded != targetInventoryExpanded)
            {
                return IssueInventoryModeAttempt(
                    currentState,
                    selection,
                    targetInventoryExpanded,
                    attemptTimeout,
                    completedEquipmentAttempts,
                    maximumAttempts,
                    currentTime,
                    snapshot,
                    panelTransition,
                    spellCast,
                    flower);
            }
        }

        return IssueStaffEquipmentAttempt(
            currentState,
            selection,
            attemptTimeout,
            checked(completedEquipmentAttempts + 1),
            maximumAttempts,
            currentTime,
            snapshot,
            panelTransition,
            spellCast,
            flower);
    }

    private static MacroDecision ContinueStaffSwitchAfterObservation(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        StaffSwitchState staffSwitch,
        SpellCastState? spellCast)
    {
        var selection = staffSwitch.Selection!;

        if (spellCast is
            {
                Status: SpellCastStatus.WaitingForStaff
            })
        {
            var refreshedPlan = ReplanSelectedSpell(
                currentState,
                spellCast,
                snapshot,
                currentTime);
            if (!DoesPlanMatchSelection(spellCast, refreshedPlan))
            {
                var nextSpellCast = refreshedPlan.HasSelection
                    ? spellCast.SelectionInvalidated(refreshedPlan)
                    : spellCast.Replanned(refreshedPlan);
                var spellQueue = refreshedPlan.HasSelection
                    ? currentState.SpellQueue
                    : refreshedPlan.Queue;

                return Changed(
                    currentState,
                    currentState.Lifecycle,
                    currentState.StopReason,
                    snapshot,
                    currentState.LastTransitionAt,
                    pendingAction: null,
                    spellQueue: spellQueue,
                    panelTransition: panelTransition,
                    staffSwitch: staffSwitch.SelectionInvalidated(),
                    spellCooldowns: refreshedPlan.Cooldowns,
                    spellCast: nextSpellCast);
            }

            spellCast = spellCast.WithPlan(refreshedPlan);
        }

        if (!IsStaffSelectionStillValid(selection, snapshot))
        {
            var nextSpellCast = spellCast is
            {
                Status: SpellCastStatus.WaitingForStaff
            } waitingForStaff
                ? waitingForStaff.StaffUnavailable()
                : spellCast;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                panelTransition: panelTransition,
                staffSwitch: staffSwitch.SelectionInvalidated(),
                spellCast: nextSpellCast);
        }

        return ContinueStaffSwitch(
            currentState,
            selection,
            staffSwitch.AttemptTimeout,
            staffSwitch.MaximumAttempts,
            staffSwitch.CompletedEquipmentAttempts,
            currentTime,
            snapshot,
            panelTransition,
            spellCast);
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
            var flower = spellCast?.Origin == SpellCastOrigin.Flower
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
                spellCast: spellCast,
                flower: flower);
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
            var flower = spellCast?.Origin == SpellCastOrigin.Flower
                ? currentState.Flower?.StaffUnavailable()
                : currentState.Flower;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                staffSwitch: staffSwitch.SelectionInvalidated(),
                spellCast: spellCast,
                flower: flower);
        }

        return ContinueStaffSwitch(
            currentState,
            selection,
            staffSwitch.AttemptTimeout,
            staffSwitch.MaximumAttempts,
            pendingAction.Attempt,
            currentTime,
            spellCast: currentState.SpellCast);
    }

    private static MacroDecision HandleInventoryModeDeadline(
        MacroState currentState,
        PendingAction pendingAction)
    {
        if (currentState.StaffSwitch is not
            {
                Status: StaffSwitchStatus.ChangingInventoryMode
            } staffSwitch ||
            staffSwitch.ActionId != pendingAction.Intent.ActionId)
        {
            return Unchanged(currentState);
        }

        var spellCast = currentState.SpellCast is
        {
            Status: SpellCastStatus.WaitingForStaff
        } waitingForStaff
            ? waitingForStaff.StaffUnavailable()
            : currentState.SpellCast;
        var flower = spellCast?.Origin == SpellCastOrigin.Flower
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
            spellCast: spellCast,
            flower: flower);
    }

    private static MacroDecision IssueInventoryModeAttempt(
        MacroState currentState,
        StaffSelection selection,
        bool targetInventoryExpanded,
        TimeSpan attemptTimeout,
        int completedEquipmentAttempts,
        int maximumEquipmentAttempts,
        MacroTimestamp currentTime,
        ClientSnapshot latestSnapshot,
        PanelTransitionState? panelTransition = null,
        SpellCastState? spellCast = null,
        FlowerState? flower = null)
    {
        var actionId = new ClientActionId(currentState.NextClientActionId);
        ClientActionIntent intent = targetInventoryExpanded
            ? new ExpandInventoryIntent(actionId)
            : new CollapseInventoryIntent(actionId);
        var deadline = currentTime.Add(attemptTimeout);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            latestSnapshot.Sequence);
        var staffSwitch = StaffSwitchState.ChangingInventoryMode(
            selection,
            attemptTimeout,
            completedEquipmentAttempts,
            maximumEquipmentAttempts,
            actionId,
            targetInventoryExpanded);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            latestSnapshot,
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

    private static MacroDecision IssueStaffEquipmentAttempt(
        MacroState currentState,
        StaffSelection selection,
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        MacroTimestamp currentTime,
        ClientSnapshot? latestSnapshot = null,
        PanelTransitionState? panelTransition = null,
        SpellCastState? spellCast = null,
        FlowerState? flower = null)
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

    private static bool CanConfirmInventoryMode(
        PendingAction? pendingAction,
        ClientSnapshot snapshot)
    {
        var targetInventoryExpanded = pendingAction?.Intent switch
        {
            ExpandInventoryIntent => true,
            CollapseInventoryIntent => false,
            _ => (bool?)null
        };

        return targetInventoryExpanded.HasValue &&
               snapshot.IsInventoryExpanded ==
               targetInventoryExpanded.Value &&
               CanSnapshotConfirmAction(pendingAction!, snapshot);
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
                StaffSwitchStatus.ExpandingInterface or
                StaffSwitchStatus.ChangingInventoryMode or
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
