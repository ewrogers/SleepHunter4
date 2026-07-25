namespace SleepHunter.Runtime.Automation.Flowering;

public readonly record struct FlowerObservationSequence
{
    public FlowerObservationSequence(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Flower observation sequences must be positive.");
        }

        Value = value;
    }

    public long Value { get; }
}
