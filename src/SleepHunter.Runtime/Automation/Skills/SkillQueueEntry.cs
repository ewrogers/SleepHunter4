namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillQueueEntry
{
    public SkillQueueEntry(SkillQueueEntryId id, string name)
    {
        if (id.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "Skill queue entries require a valid identifier.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
    }

    public SkillQueueEntryId Id { get; }

    public string Name { get; }
}
