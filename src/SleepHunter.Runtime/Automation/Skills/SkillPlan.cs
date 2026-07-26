using System.Collections.Immutable;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillPlan
{
    internal SkillPlan(
        SkillPlanStatus status,
        SkillQueueEntry? selectedEntry,
        SkillSnapshot? selectedSkill,
        SkillActionKind? actionKind,
        bool requiresDisarm,
        SkillQueueState queue,
        SkillCooldownState cooldowns,
        ImmutableArray<SkillReadiness> readiness)
    {
        Status = status;
        SelectedEntry = selectedEntry;
        SelectedSkill = selectedSkill;
        ActionKind = actionKind;
        RequiresDisarm = requiresDisarm;
        Queue = queue;
        Cooldowns = cooldowns;
        Readiness = readiness;
    }

    public SkillPlanStatus Status { get; }

    public SkillQueueEntry? SelectedEntry { get; }

    public SkillSnapshot? SelectedSkill { get; }

    public SkillActionKind? ActionKind { get; }

    public bool RequiresDisarm { get; }

    public SkillQueueState Queue { get; }

    public SkillCooldownState Cooldowns { get; }

    public ImmutableArray<SkillReadiness> Readiness { get; }

    public bool HasSelection => SelectedEntry is not null;
}
