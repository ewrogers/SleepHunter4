namespace SleepHunter.Runtime.Automation.Flowering;

public readonly record struct FlowerQueueEntryId
{
    public FlowerQueueEntryId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Flower queue entry identifiers must be positive.");
        }

        Value = value;
    }

    public long Value { get; }
}
