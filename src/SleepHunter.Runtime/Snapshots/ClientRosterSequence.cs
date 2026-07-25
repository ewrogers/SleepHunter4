namespace SleepHunter.Runtime.Snapshots;

public readonly record struct ClientRosterSequence
{
    public ClientRosterSequence(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Client roster sequences must be positive.");
        }

        Value = value;
    }

    public long Value { get; }
}
