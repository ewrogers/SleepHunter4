using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillReadiness
{
    internal SkillReadiness(
        SkillQueueEntry entry,
        SkillSnapshot? skill,
        SkillReadinessStatus status,
        MacroTimestamp? readyAt)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Skill readiness status is not supported.");
        }

        if ((status == SkillReadinessStatus.Missing) != (skill is null))
        {
            throw new ArgumentException(
                "Only missing skill readiness can omit the observed skill.",
                nameof(skill));
        }

        if (status != SkillReadinessStatus.CoolingDown &&
            readyAt is not null)
        {
            throw new ArgumentException(
                "Only cooling-down readiness can expose a ready time.",
                nameof(readyAt));
        }

        Entry = entry;
        Skill = skill;
        Status = status;
        ReadyAt = readyAt;
    }

    public SkillQueueEntry Entry { get; }

    public SkillSnapshot? Skill { get; }

    public SkillReadinessStatus Status { get; }

    public MacroTimestamp? ReadyAt { get; }
}
