namespace SleepHunter.Runtime.Automation.Skills;

public readonly record struct SkillQueueEntryId
{
    public SkillQueueEntryId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Skill queue entry identifiers must be positive.");
        }

        Value = value;
    }

    public long Value { get; }
}
