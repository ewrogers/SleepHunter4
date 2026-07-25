namespace SleepHunter.Runtime.Actions;

public readonly record struct ClientActionId
{
    public ClientActionId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Client action identifiers must be positive.");
        }

        Value = value;
    }

    public long Value { get; }
}
