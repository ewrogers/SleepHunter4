using System.Collections.Immutable;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Skills;

public static class SkillPlanner
{
    public static SkillPlan Plan(SkillPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cooldowns = request.Cooldowns.Prune(request.CurrentTime);
        if (request.Queue.Entries.IsEmpty)
        {
            return CreateWithoutSelection(
                SkillPlanStatus.QueueEmpty,
                request.Queue,
                cooldowns,
                []);
        }

        if (request.Skillbook is null)
        {
            return CreateWithoutSelection(
                SkillPlanStatus.SnapshotUnavailable,
                request.Queue,
                cooldowns,
                []);
        }

        var queuedSkills = request.Queue.Entries
            .Select(entry => request.Skillbook.Find(entry.Name))
            .Where(skill => skill is not null)
            .Cast<SkillSnapshot>()
            .ToImmutableArray();
        var requiresVitals = request.Policy.RequireMana ||
                             queuedSkills.Any(
                                 skill =>
                                     skill.HealthCondition.IsRestricted);
        if (requiresVitals && request.Vitals is null)
        {
            return CreateWithoutSelection(
                SkillPlanStatus.SnapshotUnavailable,
                request.Queue,
                cooldowns,
                []);
        }

        var readiness = request.Queue.Entries
            .Select(entry => EvaluateReadiness(
                entry,
                request.Vitals,
                request.Skillbook,
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
            var status = readiness.Any(
                entry => entry.Status is
                    SkillReadinessStatus.WaitingForHealth or
                    SkillReadinessStatus.WaitingForMana or
                    SkillReadinessStatus.CoolingDown)
                ? SkillPlanStatus.Waiting
                : SkillPlanStatus.Unavailable;
            return CreateWithoutSelection(
                status,
                queueEvaluation.State,
                cooldowns,
                readiness);
        }

        var selected = readiness.Single(
            entry => entry.Entry.Id == selectedEntry.Id);
        var selectedSkill = selected.Skill!;
        var actionKind =
            selectedSkill.IsAssail &&
            request.Policy.AssailMode == AssailMode.SpaceBar
                ? SkillActionKind.Assail
                : SkillActionKind.UseSkill;
        var requiresDisarm =
            selectedSkill.RequiresDisarm ||
            selectedSkill.IsAssail &&
            request.Policy.DisarmForAssails;

        return new SkillPlan(
            SkillPlanStatus.Ready,
            selectedEntry,
            selectedSkill,
            actionKind,
            requiresDisarm,
            queueEvaluation.State,
            cooldowns,
            readiness);
    }

    private static SkillReadiness EvaluateReadiness(
        SkillQueueEntry entry,
        VitalsSnapshot? vitals,
        SkillbookSnapshot skillbook,
        SkillCooldownState cooldowns,
        MacroTimestamp currentTime,
        SkillUsePolicy policy)
    {
        var skill = skillbook.Find(entry.Name);
        if (skill is null)
        {
            return new SkillReadiness(
                entry,
                skill: null,
                SkillReadinessStatus.Missing,
                readyAt: null);
        }

        var readyAt = cooldowns.GetReadyAt(entry.Name, currentTime);
        if (skill.IsActionDelayed || readyAt is not null)
        {
            return new SkillReadiness(
                entry,
                skill,
                SkillReadinessStatus.CoolingDown,
                readyAt);
        }

        if (skill.HealthCondition.IsRestricted &&
            !skill.HealthCondition.IsSatisfiedBy(vitals!))
        {
            return new SkillReadiness(
                entry,
                skill,
                SkillReadinessStatus.WaitingForHealth,
                readyAt: null);
        }

        if (policy.RequireMana && skill.ManaCost > vitals!.CurrentMana)
        {
            return new SkillReadiness(
                entry,
                skill,
                SkillReadinessStatus.WaitingForMana,
                readyAt: null);
        }

        return new SkillReadiness(
            entry,
            skill,
            SkillReadinessStatus.Ready,
            readyAt: null);
    }

    private static SkillQueueAvailability ToQueueAvailability(
        SkillReadiness readiness) =>
        readiness.Status switch
        {
            SkillReadinessStatus.Ready => SkillQueueAvailability.Ready,
            SkillReadinessStatus.WaitingForHealth or
                SkillReadinessStatus.WaitingForMana or
                SkillReadinessStatus.CoolingDown =>
                SkillQueueAvailability.TemporarilyUnavailable,
            _ => SkillQueueAvailability.Missing
        };

    private static SkillPlan CreateWithoutSelection(
        SkillPlanStatus status,
        SkillQueueState queue,
        SkillCooldownState cooldowns,
        ImmutableArray<SkillReadiness> readiness) =>
        new(
            status,
            selectedEntry: null,
            selectedSkill: null,
            actionKind: null,
            requiresDisarm: false,
            queue,
            cooldowns,
            readiness);
}
