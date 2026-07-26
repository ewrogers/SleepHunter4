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
    private static readonly TimeSpan SpellCancellationTimeout =
        TimeSpan.FromSeconds(1);

    private static MacroDecision Flower(
        MacroState currentState,
        FlowerCommand command,
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

        var targetPlan = FlowerPlanner.Plan(
            new FlowerPlanningRequest(
                snapshot.Client,
                snapshot.Location,
                currentState.FlowerQueue,
                currentState.FlowerSchedules,
                currentState.ClientRoster.Clients,
                currentTime,
                command.Policy.Target));
        var flower = FlowerState.FromPlan(
            targetPlan,
            command.Policy,
            currentState.ClientRoster.Sequence);
        var snapshotIsFresh =
            currentState.SpellCast?.SnapshotRequiredAfter is not
            { } requiredAfter ||
            snapshot.CaptureStartedAt > requiredAfter;

        var prioritizedWaitingCharacter =
            targetPlan.SelectionKind ==
            FlowerSelectionKind.WaitingCharacter &&
            command.Policy.Target.PrioritizeAlternateCharacters;
        if (!prioritizedWaitingCharacter &&
            command.Policy.UseVineyard)
        {
            var vineyardEntry = CreateFlowerSpellEntry(
                FlowerActionKind.Vineyard,
                SpellTarget.None);
            var vineyardPlan = PlanFlowerSpell(
                vineyardEntry,
                snapshot,
                currentState.SpellCooldowns,
                currentTime,
                command.Policy.Spell.Cast,
                snapshotIsFresh);
            if (vineyardPlan.HasSelection)
            {
                return BeginFlowerSpell(
                    currentState,
                    flower,
                    FlowerActionKind.Vineyard,
                    vineyardEntry,
                    vineyardPlan,
                    command,
                    currentTime);
            }
        }

        if (!targetPlan.HasSelection)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                flowerSchedules: targetPlan.Schedules,
                flower: flower);
        }

        if (!snapshotIsFresh)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                flowerSchedules: targetPlan.Schedules,
                flower: flower.SnapshotUnavailable());
        }

        var plantSpell = snapshot.Spellbook?.Find(FlowerSpellNames.Plant);
        if (plantSpell is null)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                flowerSchedules: targetPlan.Schedules,
                flower: flower.SpellUnavailable());
        }

        if (ShouldRestoreMana(
                command.Policy,
                snapshot.Vitals,
                plantSpell.ManaCost))
        {
            var restorationEntry = CreateFlowerSpellEntry(
                FlowerActionKind.RestoreMana,
                SpellTarget.None);
            var restorationPlan = PlanFlowerSpell(
                restorationEntry,
                snapshot,
                currentState.SpellCooldowns,
                currentTime,
                command.Policy.Spell.Cast,
                includeSnapshotSections: true);
            if (restorationPlan.HasSelection)
            {
                return BeginFlowerSpell(
                    currentState,
                    flower,
                    FlowerActionKind.RestoreMana,
                    restorationEntry,
                    restorationPlan,
                    command,
                    currentTime);
            }
        }

        var plantEntry = CreateFlowerSpellEntry(
            FlowerActionKind.Plant,
            targetPlan.SelectedTarget!);
        var plantPlan = PlanFlowerSpell(
            plantEntry,
            snapshot,
            currentState.SpellCooldowns,
            currentTime,
            command.Policy.Spell.Cast,
            includeSnapshotSections: true);
        return BeginFlowerSpell(
            currentState,
            flower,
            FlowerActionKind.Plant,
            plantEntry,
            plantPlan,
            command,
            currentTime);
    }

    private static MacroDecision BeginFlowerSpell(
        MacroState currentState,
        FlowerState flower,
        FlowerActionKind action,
        SpellQueueEntry spellEntry,
        SpellCastPlan spellPlan,
        FlowerCommand command,
        MacroTimestamp currentTime)
    {
        var spellCast = SpellCastState.FromPlan(
            spellPlan,
            command.Policy.Spell,
            currentState.SpellCast?.SnapshotRequiredAfter,
            SpellCastOrigin.Flower);
        flower = flower.WithSpell(action, spellEntry, spellCast);

        if (!spellPlan.HasSelection)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                spellCooldowns: spellPlan.Cooldowns,
                spellCast: spellCast,
                flowerSchedules: flower.Plan.Schedules,
                flower: flower);
        }

        return BeginSpellCast(
            currentState,
            spellCast,
            spellPlan,
            command.StaffCatalog.GetCandidates(action),
            currentTime,
            flower);
    }

    private static SpellCastPlan PlanFlowerSpell(
        SpellQueueEntry entry,
        ClientSnapshot snapshot,
        SpellCooldownState cooldowns,
        MacroTimestamp currentTime,
        SpellCastPolicy policy,
        bool includeSnapshotSections)
    {
        var queue = SpellQueueState.Empty.Add(entry);
        return PlanSpell(
            queue,
            snapshot,
            cooldowns,
            currentTime,
            policy,
            includeSnapshotSections);
    }

    private static SpellQueueEntry CreateFlowerSpellEntry(
        FlowerActionKind action,
        SpellTarget target)
    {
        var name = action switch
        {
            FlowerActionKind.RestoreMana =>
                FlowerSpellNames.ManaRestoration,
            FlowerActionKind.Vineyard =>
                FlowerSpellNames.Vineyard,
            FlowerActionKind.Plant =>
                FlowerSpellNames.Plant,
            _ => throw new ArgumentOutOfRangeException(
                nameof(action),
                action,
                "The flower action is not supported.")
        };
        return new SpellQueueEntry(
            new SpellQueueEntryId(checked((long)action + 1)),
            name,
            target: target);
    }

    private static bool ShouldRestoreMana(
        FlowerExecutionPolicy policy,
        VitalsSnapshot? vitals,
        int requiredMana,
        bool includeFlowerMinimum = true)
    {
        if (vitals is null)
        {
            return policy.RestoreMana ||
                   policy.RestoreManaOnDemand;
        }

        var restoreForThreshold =
            policy.RestoreMana &&
            vitals.CurrentMana < vitals.MaximumMana &&
            vitals.CurrentMana < policy.ManaRestorationThreshold;
        requiredMana = includeFlowerMinimum
            ? Math.Max(
                requiredMana,
                policy.MinimumManaBeforePlant ?? 0)
            : requiredMana;
        var restoreOnDemand =
            policy.RestoreManaOnDemand &&
            vitals.CurrentMana < requiredMana;
        return restoreForThreshold || restoreOnDemand;
    }

    private static bool ShouldCancelManaRestoration(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        if (currentState.PendingAction is not null ||
            snapshot.IsChatOpen ||
            currentState.SpellCast is not
            {
                Status: SpellCastStatus.Casting,
                Plan.SelectedSpell: { } selectedSpell
            } spellCast ||
            !string.Equals(
                selectedSpell.Name,
                FlowerSpellNames.ManaRestoration,
                StringComparison.OrdinalIgnoreCase) ||
            snapshot.Vitals is not { } vitals ||
            snapshot.Spellbook is null)
        {
            return false;
        }

        var policy = spellCast.Origin == SpellCastOrigin.Flower &&
            currentState.Flower is
            {
                Action: FlowerActionKind.RestoreMana
            } flower
                ? flower.Policy
                : currentState.Automation.FlowerPolicy;
        if (!policy.RestoreMana && !policy.RestoreManaOnDemand)
        {
            return false;
        }

        var isFlowerRestoration =
            spellCast.Origin == SpellCastOrigin.Flower;
        var requiredMana =
            isFlowerRestoration
                ? GetFlowerManaRequirement(policy, snapshot)
                : GetSpellQueueManaRequirement(
                    currentState,
                    snapshot,
                    currentTime,
                    spellCast.Policy.Cast);
        return !ShouldRestoreMana(
            policy,
            vitals,
            requiredMana,
            includeFlowerMinimum: isFlowerRestoration);
    }

    private static MacroDecision CancelManaRestoration(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        var actionId = new ClientActionId(
            currentState.NextClientActionId);
        var intent = new CancelSpellIntent(actionId);
        var deadline = currentTime.Add(SpellCancellationTimeout);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            snapshot.Sequence);

        var flower = currentState.SpellCast!.Origin ==
            SpellCastOrigin.Flower &&
            currentState.Flower is
            {
                Action: FlowerActionKind.RestoreMana
            } activeFlower
                ? activeFlower.Cancelled()
                : currentState.Flower;
        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            spellCast: currentState.SpellCast!.Cancelled(),
            flower: flower,
            nextClientActionId:
                checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static bool ShouldRestoreManaForSpellQueue(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        SpellCastPolicy spellPolicy,
        FlowerExecutionPolicy manaPolicy)
    {
        if (!manaPolicy.RestoreMana &&
            !manaPolicy.RestoreManaOnDemand ||
            snapshot.Vitals is null)
        {
            return false;
        }

        var requiredMana = GetSpellQueueManaRequirement(
            currentState,
            snapshot,
            currentTime,
            spellPolicy);
        return ShouldRestoreMana(
            manaPolicy,
            snapshot.Vitals,
            requiredMana,
            includeFlowerMinimum: false);
    }

    private static int GetFlowerManaRequirement(
        FlowerExecutionPolicy policy,
        ClientSnapshot snapshot)
    {
        var plantMana = snapshot.Spellbook?
            .Find(FlowerSpellNames.Plant)?
            .ManaCost ?? 0;
        return plantMana;
    }

    private static int GetSpellQueueManaRequirement(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        SpellCastPolicy policy)
    {
        var queue = currentState.SpellQueue;
        foreach (var entry in queue.Entries.Where(
                     entry => string.Equals(
                         entry.Name,
                         FlowerSpellNames.ManaRestoration,
                         StringComparison.OrdinalIgnoreCase)))
        {
            queue = queue.Remove(entry.Id);
        }

        var withoutManaRequirement = new SpellCastPolicy(
            requireMana: false,
            policy.Timing,
            policy.SkipCoolingDownSpells);
        var plan = PlanSpell(
            queue,
            snapshot,
            currentState.SpellCooldowns,
            currentTime,
            withoutManaRequirement);
        return plan.SelectedSpell?.ManaCost ?? 0;
    }

    private static FlowerPlan ReplanFlower(
        MacroState currentState,
        FlowerState flower,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime) =>
        FlowerPlanner.Plan(
            new FlowerPlanningRequest(
                snapshot.Client,
                snapshot.Location,
                currentState.FlowerQueue,
                currentState.FlowerSchedules,
                currentState.ClientRoster.Clients,
                currentTime,
                flower.Policy.Target));

    private static bool DoesFlowerPlanMatchSelection(
        FlowerState flower,
        FlowerPlan plan)
    {
        if (flower.Action == FlowerActionKind.Vineyard)
        {
            return true;
        }

        var expected = flower.Plan;
        return plan.SelectionKind == expected.SelectionKind &&
               plan.SelectedEntry == expected.SelectedEntry &&
               plan.SelectedTarget == expected.SelectedTarget &&
               string.Equals(
                   plan.SelectedClient?.Client.InstanceId,
                   expected.SelectedClient?.Client.InstanceId,
                   StringComparison.Ordinal);
    }

    private static MacroDecision FinishInvalidFlowerSelection(
        MacroState currentState,
        FlowerState flower,
        FlowerPlan plan,
        SpellCastState spellCast,
        ClientSnapshot snapshot,
        PanelTransitionState? panelTransition,
        StaffSwitchState? staffSwitch)
    {
        var invalidSpellCast =
            spellCast.SelectionInvalidated(spellCast.Plan);
        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction: null,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCast: invalidSpellCast,
            flowerSchedules: plan.Schedules,
            flower: flower.SelectionInvalidated(
                plan,
                currentState.ClientRoster.Sequence));
    }

    private static MacroDecision FinishInvalidFlowerSpell(
        MacroState currentState,
        FlowerState flower,
        SpellCastState spellCast,
        SpellCastPlan plan,
        ClientSnapshot snapshot,
        PanelTransitionState? panelTransition,
        StaffSwitchState? staffSwitch)
    {
        var nextSpellCast = plan.HasSelection
            ? spellCast.SelectionInvalidated(plan)
            : spellCast.Replanned(plan);
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
            spellCast: nextSpellCast,
            flower: flower.WithSpellCast(nextSpellCast));
    }

    private static MacroDecision HandleClientRoster(
        MacroState currentState,
        ClientRosterSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        if (snapshot.Sequence is not { } sequence ||
            snapshot.CapturedAt > currentTime ||
            currentState.ClientRoster.Sequence is { } currentSequence &&
            sequence.Value <= currentSequence.Value)
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
            clientRoster: snapshot);
    }

    private static MacroDecision ChangeFlowerQueue(
        MacroState currentState,
        FlowerQueueState flowerQueue,
        MacroTimestamp currentTime)
    {
        var schedules = currentState.FlowerSchedules.Synchronize(
            flowerQueue,
            currentTime);
        var targetRotations = currentState.FlowerTargetRotations.Synchronize(
            flowerQueue.Entries.Select(entry =>
                KeyValuePair.Create(entry.Id.Value, entry.Target)));
        if (currentState.FlowerQueue.Equals(flowerQueue) &&
            currentState.FlowerSchedules.Equals(schedules) &&
            currentState.FlowerTargetRotations.Equals(targetRotations))
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
            flowerQueue: flowerQueue,
            flowerSchedules: schedules,
            flowerTargetRotations: targetRotations);
    }

    private static FlowerState? CancelPendingFlower(
        MacroState currentState) =>
        currentState.Flower is
        {
            Status: FlowerStatus.WaitingForStaff or
                FlowerStatus.WaitingForPanel or
                FlowerStatus.Casting
        } flower
            ? flower.Cancelled()
            : currentState.Flower;
}
