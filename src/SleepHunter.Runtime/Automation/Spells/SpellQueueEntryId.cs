namespace SleepHunter.Runtime.Automation.Spells;

public readonly record struct SpellQueueEntryId
{
    public SpellQueueEntryId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Spell queue entry identifiers must be positive.");
        }

        Value = value;
    }

    public long Value { get; }
}
