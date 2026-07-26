namespace SleepHunter.Runtime.Time;

public readonly record struct MacroTimestamp : IComparable<MacroTimestamp>
{
    public static MacroTimestamp Zero { get; } = new(TimeSpan.Zero);

    public MacroTimestamp(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsed),
                elapsed,
                "Macro time cannot be negative.");
        }

        Elapsed = elapsed;
    }

    public TimeSpan Elapsed { get; }

    public MacroTimestamp Add(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "Macro time can only advance.");
        }

        return new MacroTimestamp(Elapsed + duration);
    }

    public int CompareTo(MacroTimestamp other) => Elapsed.CompareTo(other.Elapsed);

    public static bool operator <(MacroTimestamp left, MacroTimestamp right) =>
        left.CompareTo(right) < 0;

    public static bool operator <=(MacroTimestamp left, MacroTimestamp right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >(MacroTimestamp left, MacroTimestamp right) =>
        left.CompareTo(right) > 0;

    public static bool operator >=(MacroTimestamp left, MacroTimestamp right) =>
        left.CompareTo(right) >= 0;
}
