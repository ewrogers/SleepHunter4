using SleepHunter.Runtime.Automation;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellQueueEntry
{
    public SpellQueueEntry(
        SpellQueueEntryId id,
        string name,
        int? targetLevel = null,
        SpellTarget? target = null,
        HealthCondition? healthCondition = null)
    {
        if (id.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "Spell queue entries require a valid identifier.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (targetLevel is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetLevel),
                targetLevel,
                "Target levels must be positive.");
        }

        Id = id;
        Name = name.Trim();
        TargetLevel = targetLevel;
        Target = target ?? SpellTarget.None;
        HealthCondition =
            healthCondition ?? SleepHunter.Runtime.Automation.HealthCondition.Any;
    }

    public SpellQueueEntryId Id { get; }

    public string Name { get; }

    public int? TargetLevel { get; }

    public SpellTarget Target { get; }

    public HealthCondition HealthCondition { get; }
}
