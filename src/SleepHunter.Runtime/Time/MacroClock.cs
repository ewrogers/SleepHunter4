namespace SleepHunter.Runtime.Time;

public sealed class MacroClock
{
    private readonly long originTimestamp;
    private readonly TimeProvider timeProvider;

    public MacroClock(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.timeProvider = timeProvider;
        originTimestamp = timeProvider.GetTimestamp();
    }

    internal TimeProvider TimeProvider => timeProvider;

    public MacroTimestamp GetCurrentTimestamp()
    {
        var elapsed = timeProvider.GetElapsedTime(
            originTimestamp,
            timeProvider.GetTimestamp());

        return new MacroTimestamp(elapsed);
    }
}
