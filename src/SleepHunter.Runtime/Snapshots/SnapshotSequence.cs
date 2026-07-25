namespace SleepHunter.Runtime.Snapshots;

public readonly record struct SnapshotSequence : IComparable<SnapshotSequence>
{
    public SnapshotSequence(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Snapshot sequences must be positive.");
        }

        Value = value;
    }

    public long Value { get; }

    public int CompareTo(SnapshotSequence other) => Value.CompareTo(other.Value);

    public static bool operator <(SnapshotSequence left, SnapshotSequence right) =>
        left.CompareTo(right) < 0;

    public static bool operator <=(SnapshotSequence left, SnapshotSequence right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >(SnapshotSequence left, SnapshotSequence right) =>
        left.CompareTo(right) > 0;

    public static bool operator >=(SnapshotSequence left, SnapshotSequence right) =>
        left.CompareTo(right) >= 0;
}
