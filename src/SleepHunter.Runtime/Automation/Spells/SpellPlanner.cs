using System.Collections.Immutable;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Spells;

public static class SpellPlanner
{
    public static SpellCastPlan Plan(SpellPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cooldowns = request.Cooldowns.Prune(request.CurrentTime);
        if (request.Queue.Entries.IsEmpty)
        {
            return CreateWithoutSelection(
                SpellPlanStatus.QueueEmpty,
                request.Queue,
                cooldowns,
                []);
        }

        var requiresVitals =
            request.Policy.RequireMana ||
            request.Queue.Entries.Any(
                entry => entry.HealthCondition.IsRestricted);
        if (request.Spellbook is null ||
            requiresVitals && request.Vitals is null)
        {
            return CreateWithoutSelection(
                SpellPlanStatus.SnapshotUnavailable,
                request.Queue,
                cooldowns,
                []);
        }

        var readiness = request.Queue.Entries
            .Select(entry => EvaluateReadiness(
                entry,
                request.Vitals,
                request.Spellbook,
                cooldowns,
                request.CurrentTime,
                request.Policy))
            .ToImmutableArray();
        var availability = readiness.ToDictionary(
            entry => entry.Entry.Id,
            ToQueueAvailability);
        var queueEvaluation = request.Queue.EvaluateNext(availability);

        if (queueEvaluation.SelectedEntry is not { } selectedEntry)
        {
            return CreateWithoutSelection(
                GetUnavailableStatus(readiness),
                queueEvaluation.State,
                cooldowns,
                readiness);
        }

        var selected = readiness.Single(
            entry => entry.Entry.Id == selectedEntry.Id);
        var castDuration = request.Policy.Timing.CalculateDuration(
            selected.Spell!.CastLines);

        return new SpellCastPlan(
            SpellPlanStatus.Ready,
            selectedEntry,
            selected.Spell,
            castDuration,
            queueEvaluation.State,
            cooldowns,
            readiness);
    }

    private static SpellReadiness EvaluateReadiness(
        SpellQueueEntry entry,
        VitalsSnapshot? vitals,
        SpellbookSnapshot spellbook,
        SpellCooldownState cooldowns,
        MacroTimestamp currentTime,
        SpellCastPolicy policy)
    {
        var spell = spellbook.Find(entry.Name);
        if (spell is null)
        {
            return new SpellReadiness(
                entry,
                spell: null,
                SpellReadinessStatus.Missing,
                readyAt: null);
        }

        if (entry.TargetLevel is { } targetLevel &&
            spell.CurrentLevel >= targetLevel)
        {
            return new SpellReadiness(
                entry,
                spell,
                SpellReadinessStatus.Complete,
                readyAt: null);
        }

        if (entry.TargetLevel is { } unavailableTarget &&
            spell.MaximumLevel > 0 &&
            unavailableTarget > spell.MaximumLevel)
        {
            return new SpellReadiness(
                entry,
                spell,
                SpellReadinessStatus.TargetLevelUnavailable,
                readyAt: null);
        }

        var readyAt = cooldowns.GetReadyAt(entry.Name, currentTime);
        if (spell.IsActionDelayed || readyAt is not null)
        {
            return new SpellReadiness(
                entry,
                spell,
                SpellReadinessStatus.CoolingDown,
                readyAt);
        }

        if (entry.HealthCondition.IsRestricted &&
            !entry.HealthCondition.IsSatisfiedBy(vitals!))
        {
            return new SpellReadiness(
                entry,
                spell,
                SpellReadinessStatus.WaitingForHealth,
                readyAt: null);
        }

        if (policy.RequireMana && spell.ManaCost > vitals!.CurrentMana)
        {
            return new SpellReadiness(
                entry,
                spell,
                SpellReadinessStatus.WaitingForMana,
                readyAt: null);
        }

        return new SpellReadiness(
            entry,
            spell,
            SpellReadinessStatus.Ready,
            readyAt: null);
    }

    private static SpellQueueAvailability ToQueueAvailability(
        SpellReadiness readiness) =>
        readiness.Status switch
        {
            SpellReadinessStatus.Ready => SpellQueueAvailability.Ready,
            SpellReadinessStatus.WaitingForHealth or
                SpellReadinessStatus.WaitingForMana or
                SpellReadinessStatus.CoolingDown =>
                SpellQueueAvailability.TemporarilyUnavailable,
            SpellReadinessStatus.Complete or
                SpellReadinessStatus.TargetLevelUnavailable =>
                SpellQueueAvailability.Complete,
            _ => SpellQueueAvailability.Missing
        };

    private static SpellPlanStatus GetUnavailableStatus(
        ImmutableArray<SpellReadiness> readiness)
    {
        if (readiness.Any(
                entry => entry.Status is
                    SpellReadinessStatus.WaitingForHealth or
                    SpellReadinessStatus.WaitingForMana or
                    SpellReadinessStatus.CoolingDown))
        {
            return SpellPlanStatus.Waiting;
        }

        if (readiness.All(
                entry => entry.Status == SpellReadinessStatus.Complete))
        {
            return SpellPlanStatus.Complete;
        }

        return SpellPlanStatus.Unavailable;
    }

    private static SpellCastPlan CreateWithoutSelection(
        SpellPlanStatus status,
        SpellQueueState queue,
        SpellCooldownState cooldowns,
        ImmutableArray<SpellReadiness> readiness) =>
        new(
            status,
            selectedEntry: null,
            selectedSpell: null,
            castDuration: null,
            queue,
            cooldowns,
            readiness);
}
