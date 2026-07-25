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
    private static MacroDecision CastNextSpell(
        MacroState currentState,
        CastNextSpellCommand command,
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

        var snapshotIsFresh =
            currentState.SpellCast?.SnapshotRequiredAfter is not
            { } requiredAfter ||
            snapshot.CaptureStartedAt > requiredAfter;
        var plan = PlanSpell(
            currentState.SpellQueue,
            snapshot,
            currentState.SpellCooldowns,
            currentTime,
            command.Policy.Cast,
            snapshotIsFresh);
        var spellCast = SpellCastState.FromPlan(
            plan,
            command.Policy,
            currentState.SpellCast?.SnapshotRequiredAfter);

        if (!plan.HasSelection)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                spellQueue: plan.Queue,
                spellCooldowns: plan.Cooldowns,
                spellCast: spellCast);
        }

        var candidates = command.StaffCatalog.GetCandidates(
            plan.SelectedEntry!.Id);
        if (command.Policy.AllowStaffSwitching && !candidates.IsEmpty)
        {
            if (snapshot.Character is null ||
                snapshot.Inventory is null ||
                snapshot.Equipment is null)
            {
                return Changed(
                    currentState,
                    currentState.Lifecycle,
                    currentState.StopReason,
                    currentState.LatestSnapshot,
                    currentState.LastTransitionAt,
                    pendingAction: null,
                    spellCooldowns: plan.Cooldowns,
                    spellCast: spellCast.SnapshotUnavailable());
            }

            var staffSelection = StaffSelector.Select(
                new StaffSelectionRequest(
                    plan.SelectedSpell!.CastLines,
                    snapshot.Character,
                    snapshot.Inventory,
                    snapshot.Equipment,
                    candidates));
            spellCast = spellCast.WithStaffSelection(staffSelection);
            var staffSwitch = StaffSwitchState.NoChange(staffSelection);

            if (staffSelection.Action != StaffSelectionAction.None)
            {
                return BeginStaffSwitch(
                    currentState,
                    staffSelection,
                    command.Policy.StaffEquipment,
                    currentTime,
                    spellCast.WaitingForStaff(staffSelection));
            }

            return ContinueSpellCastToPanel(
                currentState,
                spellCast,
                plan,
                snapshot,
                currentTime,
                currentState.PanelTransition,
                staffSwitch);
        }

        return ContinueSpellCastToPanel(
            currentState,
            spellCast,
            plan,
            snapshot,
            currentTime,
            currentState.PanelTransition,
            currentState.StaffSwitch);
    }

    private static MacroDecision ContinueSpellCastToPanel(
        MacroState currentState,
        SpellCastState spellCast,
        SpellCastPlan plan,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        StaffSwitchState? staffSwitch)
    {
        if (!snapshot.ActivePanel.IsEquivalentTo(plan.SelectedSpell!.Panel))
        {
            spellCast = spellCast.WaitingForPanel(plan);
            return IssuePanelTransitionAttempt(
                currentState,
                plan.SelectedSpell.Panel,
                spellCast.Policy.PanelTransition.AttemptTimeout,
                attempt: 1,
                spellCast.Policy.PanelTransition.MaximumAttempts,
                currentTime,
                staffSwitch,
                spellCast,
                plan.Cooldowns);
        }

        return IssueCastSpell(
            currentState,
            spellCast,
            plan,
            snapshot,
            currentTime,
            panelTransition,
            staffSwitch);
    }

    private static MacroDecision ContinueSpellCastAfterPanel(
        MacroState currentState,
        SpellCastState spellCast,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState panelTransition,
        StaffSwitchState? staffSwitch)
    {
        var plan = ReplanSelectedSpell(
            currentState,
            spellCast,
            snapshot,
            currentTime);

        if (!DoesPlanMatchSelection(spellCast, plan))
        {
            var nextSpellCast = plan.HasSelection
                ? spellCast.SelectionInvalidated(plan)
                : spellCast.Replanned(plan);
            var spellQueue = plan.HasSelection
                ? currentState.SpellQueue
                : plan.Queue;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                spellQueue: spellQueue,
                panelTransition: panelTransition,
                staffSwitch: staffSwitch,
                spellCooldowns: plan.Cooldowns,
                spellCast: nextSpellCast);
        }

        return ContinueSpellCastToPanel(
            currentState,
            spellCast.WithPlan(plan),
            plan,
            snapshot,
            currentTime,
            panelTransition,
            staffSwitch);
    }

    private static MacroDecision ContinueSpellCastAfterStaff(
        MacroState currentState,
        SpellCastState spellCast,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        StaffSwitchState staffSwitch)
    {
        var plan = ReplanSelectedSpell(
            currentState,
            spellCast,
            snapshot,
            currentTime);

        if (!DoesPlanMatchSelection(spellCast, plan))
        {
            var nextSpellCast = plan.HasSelection
                ? spellCast.SelectionInvalidated(plan)
                : spellCast.Replanned(plan);
            var spellQueue = plan.HasSelection
                ? currentState.SpellQueue
                : plan.Queue;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                spellQueue: spellQueue,
                panelTransition: panelTransition,
                staffSwitch: staffSwitch,
                spellCooldowns: plan.Cooldowns,
                spellCast: nextSpellCast);
        }

        return ContinueSpellCastToPanel(
            currentState,
            spellCast.WithPlan(plan),
            plan,
            snapshot,
            currentTime,
            panelTransition,
            staffSwitch);
    }

    private static MacroDecision IssueCastSpell(
        MacroState currentState,
        SpellCastState spellCast,
        SpellCastPlan plan,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        StaffSwitchState? staffSwitch)
    {
        var selectedEntry = plan.SelectedEntry!;
        var selectedSpell = plan.SelectedSpell!;
        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = new CastSpellIntent(
            actionId,
            selectedSpell.Name,
            selectedSpell.Slot,
            selectedSpell.Panel,
            selectedEntry.Target);
        var deadline = currentTime.Add(spellCast.CastDuration!.Value);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            snapshot.Sequence);
        var casting = spellCast.Casting(plan, actionId, deadline);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            spellQueue: plan.Queue,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCooldowns: plan.Cooldowns,
            spellCast: casting,
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static MacroDecision HandleSpellCastDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        CastSpellIntent intent,
        MacroTimestamp currentTime)
    {
        if (currentState.SpellCast is not
            {
                Status: SpellCastStatus.Casting,
                Plan.SelectedSpell: { } spell
            } spellCast ||
            spellCast.ActionId != intent.ActionId)
        {
            return Unchanged(currentState);
        }

        var cooldowns = currentState.SpellCooldowns.Prune(currentTime);
        var readyAt = pendingAction.Deadline.Add(spell.Cooldown);
        if (readyAt > currentTime)
        {
            cooldowns = cooldowns.WithCooldown(spell.Name, readyAt);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            pendingAction: null,
            spellCooldowns: cooldowns,
            spellCast: spellCast.Succeeded());
    }

    private static SpellCastPlan PlanSpell(
        SpellQueueState queue,
        ClientSnapshot snapshot,
        SpellCooldownState cooldowns,
        MacroTimestamp currentTime,
        SpellCastPolicy policy,
        bool includeSnapshotSections = true) =>
        SpellPlanner.Plan(
            new SpellPlanningRequest(
                queue,
                includeSnapshotSections ? snapshot.Vitals : null,
                includeSnapshotSections ? snapshot.Spellbook : null,
                cooldowns,
                currentTime,
                policy));

    private static SpellCastPlan ReplanSelectedSpell(
        MacroState currentState,
        SpellCastState spellCast,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime) =>
        PlanSpell(
            currentState.SpellQueue,
            snapshot,
            currentState.SpellCooldowns,
            currentTime,
            spellCast.Policy.Cast);

    private static bool DoesPlanMatchSelection(
        SpellCastState spellCast,
        SpellCastPlan plan)
    {
        var expectedEntry = spellCast.Plan.SelectedEntry!;
        var expectedSpell = spellCast.Plan.SelectedSpell!;

        return plan.SelectedEntry == expectedEntry &&
               plan.SelectedSpell is { } selectedSpell &&
               selectedSpell.Slot == expectedSpell.Slot &&
               selectedSpell.CastLines == expectedSpell.CastLines &&
               string.Equals(
                   selectedSpell.Name,
                   expectedSpell.Name,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static SpellCastState? CancelPendingSpellCast(
        MacroState currentState) =>
        currentState.SpellCast is
        {
            Status: SpellCastStatus.WaitingForPanel or
                SpellCastStatus.WaitingForStaff or
                SpellCastStatus.Casting
        } spellCast
            ? spellCast.Cancelled()
            : currentState.SpellCast;
}
