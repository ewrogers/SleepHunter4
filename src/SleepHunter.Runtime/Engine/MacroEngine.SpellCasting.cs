using System.Collections.Immutable;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
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
    private static MacroDecision CastNextSpell(
        MacroState currentState,
        CastNextSpellCommand command,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not null ||
            currentState.SpellCast is
            {
                Status: SpellCastStatus.Casting
            } ||
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

        return BeginSpellCast(
            currentState,
            spellCast,
            plan,
            command.StaffCatalog.GetCandidates(plan.SelectedEntry!.Id),
            currentTime);
    }

    private static MacroDecision BeginSpellCast(
        MacroState currentState,
        SpellCastState spellCast,
        SpellCastPlan plan,
        ImmutableArray<StaffCandidate> candidates,
        MacroTimestamp currentTime,
        FlowerState? flower = null)
    {
        var snapshot = currentState.LatestSnapshot!;

        if (spellCast.Policy.AllowStaffSwitching && !candidates.IsEmpty)
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
                    spellCast: spellCast.SnapshotUnavailable(),
                    flowerSchedules: flower?.Plan.Schedules,
                    flower: flower?.SnapshotUnavailable());
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
                    spellCast.Policy.StaffEquipment,
                    currentTime,
                    spellCast.WaitingForStaff(staffSelection),
                    flower?.WithSpellCast(
                        spellCast.WaitingForStaff(staffSelection)));
            }

            return ContinueSpellCastToPanel(
                currentState,
                spellCast,
                plan,
                snapshot,
                currentTime,
                currentState.PanelTransition,
                staffSwitch,
                flower);
        }

        return ContinueSpellCastToPanel(
            currentState,
            spellCast,
            plan,
            snapshot,
            currentTime,
            currentState.PanelTransition,
            currentState.StaffSwitch,
            flower);
    }

    private static MacroDecision ContinueSpellCastToPanel(
        MacroState currentState,
        SpellCastState spellCast,
        SpellCastPlan plan,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        StaffSwitchState? staffSwitch,
        FlowerState? flower = null)
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
                plan.Cooldowns,
                flower: flower?.WithSpellCast(spellCast));
        }

        return IssueCastSpell(
            currentState,
            spellCast,
            plan,
            snapshot,
            currentTime,
            panelTransition,
            staffSwitch,
            flower);
    }

    private static MacroDecision ContinueSpellCastAfterPanel(
        MacroState currentState,
        SpellCastState spellCast,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState panelTransition,
        StaffSwitchState? staffSwitch)
    {
        var flower = currentState.Flower;
        if (spellCast.Origin == SpellCastOrigin.Flower)
        {
            if (flower is null)
            {
                return Unchanged(currentState);
            }

            var flowerPlan = ReplanFlower(
                currentState,
                flower,
                snapshot,
                currentTime);
            if (!DoesFlowerPlanMatchSelection(flower, flowerPlan))
            {
                return FinishInvalidFlowerSelection(
                    currentState,
                    flower,
                    flowerPlan,
                    spellCast,
                    snapshot,
                    panelTransition,
                    staffSwitch);
            }

            flower = flower.WithPlan(
                flowerPlan,
                currentState.ClientRoster.Sequence);
        }

        var plan = ReplanSelectedSpell(
            currentState,
            spellCast,
            snapshot,
            currentTime);

        if (!DoesPlanMatchSelection(spellCast, plan))
        {
            if (flower is not null &&
                spellCast.Origin == SpellCastOrigin.Flower)
            {
                return FinishInvalidFlowerSpell(
                    currentState,
                    flower,
                    spellCast,
                    plan,
                    snapshot,
                    panelTransition,
                    staffSwitch);
            }

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
            staffSwitch,
            flower);
    }

    private static MacroDecision ContinueSpellCastAfterStaff(
        MacroState currentState,
        SpellCastState spellCast,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        StaffSwitchState staffSwitch)
    {
        var flower = currentState.Flower;
        if (spellCast.Origin == SpellCastOrigin.Flower)
        {
            if (flower is null)
            {
                return Unchanged(currentState);
            }

            var flowerPlan = ReplanFlower(
                currentState,
                flower,
                snapshot,
                currentTime);
            if (!DoesFlowerPlanMatchSelection(flower, flowerPlan))
            {
                return FinishInvalidFlowerSelection(
                    currentState,
                    flower,
                    flowerPlan,
                    spellCast,
                    snapshot,
                    panelTransition,
                    staffSwitch);
            }

            flower = flower.WithPlan(
                flowerPlan,
                currentState.ClientRoster.Sequence);
        }

        var plan = ReplanSelectedSpell(
            currentState,
            spellCast,
            snapshot,
            currentTime);

        if (!DoesPlanMatchSelection(spellCast, plan))
        {
            if (flower is not null &&
                spellCast.Origin == SpellCastOrigin.Flower)
            {
                return FinishInvalidFlowerSpell(
                    currentState,
                    flower,
                    spellCast,
                    plan,
                    snapshot,
                    panelTransition,
                    staffSwitch);
            }

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
            staffSwitch,
            flower);
    }

    private static MacroDecision IssueCastSpell(
        MacroState currentState,
        SpellCastState spellCast,
        SpellCastPlan plan,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        StaffSwitchState? staffSwitch,
        FlowerState? flower = null)
    {
        var selectedEntry = plan.SelectedEntry!;
        var selectedSpell = plan.SelectedSpell!;
        var targetResolution = ResolveCastTarget(
            currentState,
            spellCast,
            selectedEntry.Target,
            flower);
        var targetLocation = TargetLocator.Locate(
            targetResolution.Target,
            snapshot,
            currentState.ClientRoster);
        if (!targetLocation.IsResolved)
        {
            var unavailable =
                spellCast.TargetUnavailable(targetLocation.Status);
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                panelTransition: panelTransition,
                staffSwitch: staffSwitch,
                spellCooldowns: plan.Cooldowns,
                spellCast: unavailable,
                flower: flower?.WithSpellCast(unavailable));
        }

        var locatedTarget = targetLocation.Target!;
        var spellTargetRotations = currentState.SpellTargetRotations;
        var flowerTargetRotations = currentState.FlowerTargetRotations;
        if (spellCast.Origin == SpellCastOrigin.SpellQueue)
        {
            spellTargetRotations = spellTargetRotations.Advance(
                selectedEntry.Id.Value,
                selectedEntry.Target,
                targetResolution);
        }
        else if (flower is
        {
            Action: FlowerActionKind.Plant,
            Plan:
            {
                SelectionKind: FlowerSelectionKind.QueueEntry,
                SelectedEntry: { } selectedFlowerEntry
            }
        })
        {
            flowerTargetRotations = flowerTargetRotations.Advance(
                selectedFlowerEntry.Id.Value,
                selectedFlowerEntry.Target,
                targetResolution);
        }

        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = new CastSpellIntent(
            actionId,
            selectedSpell.Name,
            selectedSpell.Slot,
            selectedSpell.Panel,
            locatedTarget);
        var deadline = currentTime.Add(spellCast.CastDuration!.Value);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            snapshot.Sequence);
        var casting = spellCast.Casting(
            plan,
            actionId,
            deadline,
            locatedTarget);
        var nextFlower = flower?.WithSpellCast(casting);
        var dialog = currentState.Dialog;
        var scheduledEvents = ImmutableArray.Create(
            new ScheduledMacroEvent(
                new ClientActionDeadlineElapsed(actionId),
                deadline));
        if (selectedSpell.OpensDialog)
        {
            var dialogDueAt =
                currentTime.Add(spellCast.Policy.Dialog.CloseDelay);
            dialog = DialogState.Scheduled(
                spellCast.Policy.Dialog,
                dialogDueAt);
            scheduledEvents = scheduledEvents.Add(
                new ScheduledMacroEvent(
                    new DialogCloseDue(dialogDueAt),
                    dialogDueAt));
        }

        var flowerQueue = currentState.FlowerQueue;
        var flowerSchedules =
            nextFlower?.Plan.Schedules ??
            currentState.FlowerSchedules;
        if (nextFlower is
            {
                Action: FlowerActionKind.Plant,
                Plan: { } flowerPlan
            })
        {
            flowerQueue = flowerPlan.Queue;
            if (flowerPlan.SelectedEntry is { } selectedFlowerEntry)
            {
                flowerSchedules = flowerSchedules.RecordUse(
                    selectedFlowerEntry,
                    currentTime);
            }

            nextFlower = nextFlower.Casting(currentTime);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            spellQueue: spellCast.Origin == SpellCastOrigin.SpellQueue
                ? plan.Queue
                : currentState.SpellQueue,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCooldowns: plan.Cooldowns,
            spellCast: casting,
            dialog: dialog,
            flowerQueue: flowerQueue,
            flowerSchedules: flowerSchedules,
            flower: nextFlower,
            spellTargetRotations: spellTargetRotations,
            flowerTargetRotations: flowerTargetRotations,
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents: scheduledEvents);
    }

    private static TargetResolution ResolveCastTarget(
        MacroState currentState,
        SpellCastState spellCast,
        SpellTarget target,
        FlowerState? flower)
    {
        if (spellCast.Origin == SpellCastOrigin.SpellQueue)
        {
            return currentState.SpellTargetRotations.Resolve(
                spellCast.Plan.SelectedEntry!.Id.Value,
                target);
        }

        if (flower is
            {
                Action: FlowerActionKind.Plant,
                Plan:
                {
                    SelectionKind: FlowerSelectionKind.QueueEntry,
                    SelectedEntry: { } selectedFlowerEntry
                }
            })
        {
            return currentState.FlowerTargetRotations.Resolve(
                selectedFlowerEntry.Id.Value,
                target);
        }

        return TargetResolver.Resolve(target);
    }

    private static MacroDecision HandleSpellCastDeadline(
        MacroState currentState,
        SpellCastState spellCast,
        MacroTimestamp currentTime)
    {
        if (spellCast is not
            {
                Status: SpellCastStatus.Casting,
                Plan.SelectedSpell: { } spell,
                CompletesAt: { } completesAt
            })
        {
            return Unchanged(currentState);
        }

        var cooldowns = currentState.SpellCooldowns.Prune(currentTime);
        var readyAt = completesAt.Add(spell.Cooldown);
        if (readyAt > currentTime)
        {
            cooldowns = cooldowns.WithCooldown(spell.Name, readyAt);
        }

        var succeeded = spellCast.Succeeded();
        var flower = currentState.Flower;
        if (spellCast.Origin == SpellCastOrigin.Flower &&
            flower is not null)
        {
            flower = flower.Succeeded(
                flower.Action == FlowerActionKind.Plant
                    ? completesAt
                    : null);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            currentState.PendingAction,
            spellCooldowns: cooldowns,
            spellCast: succeeded,
            flower: flower);
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
            spellCast.Origin == SpellCastOrigin.Flower
                ? spellCast.Plan.Queue
                : currentState.SpellQueue,
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
               selectedSpell.OpensDialog == expectedSpell.OpensDialog &&
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
