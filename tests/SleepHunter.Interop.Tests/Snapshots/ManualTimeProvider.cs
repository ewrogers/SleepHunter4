namespace SleepHunter.Interop.Tests.Snapshots;

internal sealed class ManualTimeProvider : TimeProvider
{
    private long timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override DateTimeOffset GetUtcNow() =>
        DateTimeOffset.UnixEpoch + TimeSpan.FromTicks(timestamp);

    public override long GetTimestamp() => timestamp;

    public void Advance(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "Virtual time can only advance.");
        }

        timestamp = checked(timestamp + duration.Ticks);
    }
}
