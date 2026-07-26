namespace SleepHunter.Tests.Support;

internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object sync = new();
    private readonly List<ManualTimer> timers = [];
    private long timestamp;

    public override long TimestampFrequency =>
        TimeSpan.TicksPerSecond;

    public int ActiveTimerCount
    {
        get
        {
            lock (sync)
            {
                return timers.Count;
            }
        }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (sync)
        {
            return DateTimeOffset.UnixEpoch +
                   TimeSpan.FromTicks(timestamp);
        }
    }

    public override long GetTimestamp()
    {
        lock (sync)
        {
            return timestamp;
        }
    }

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return new ManualTimer(
            this,
            callback,
            state,
            dueTime,
            period);
    }

    public void Advance(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "Virtual time can only advance.");
        }

        ManualTimer[] dueTimers;
        lock (sync)
        {
            timestamp = checked(timestamp + duration.Ticks);
            dueTimers = timers
                .Where(timer => timer.MarkDue(timestamp))
                .ToArray();
        }

        foreach (var timer in dueTimers)
            timer.Fire();
    }

    private static void ValidateTimerDuration(
        TimeSpan duration,
        string parameterName)
    {
        if (duration < TimeSpan.Zero &&
            duration != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                duration,
                "Timer durations cannot be negative.");
        }
    }

    private sealed class ManualTimer : ITimer
    {
        private readonly TimerCallback callback;
        private readonly ManualTimeProvider owner;
        private readonly object? state;

        private bool isDisposed;
        private long dueTimestamp;
        private long periodTicks;

        public ManualTimer(
            ManualTimeProvider owner,
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            ValidateTimerDuration(
                dueTime,
                nameof(dueTime));
            ValidateTimerDuration(
                period,
                nameof(period));

            this.owner = owner;
            this.callback = callback;
            this.state = state;

            lock (owner.sync)
            {
                dueTimestamp = GetDueTimestamp(
                    owner.timestamp,
                    dueTime);
                periodTicks = GetPeriodTicks(period);
                owner.timers.Add(this);
            }
        }

        public bool Change(
            TimeSpan dueTime,
            TimeSpan period)
        {
            ValidateTimerDuration(
                dueTime,
                nameof(dueTime));
            ValidateTimerDuration(
                period,
                nameof(period));

            lock (owner.sync)
            {
                if (isDisposed)
                    return false;

                dueTimestamp = GetDueTimestamp(
                    owner.timestamp,
                    dueTime);
                periodTicks = GetPeriodTicks(period);
                return true;
            }
        }

        public void Dispose()
        {
            lock (owner.sync)
            {
                if (isDisposed)
                    return;

                isDisposed = true;
                owner.timers.Remove(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public bool MarkDue(long currentTimestamp)
        {
            if (isDisposed ||
                dueTimestamp > currentTimestamp)
            {
                return false;
            }

            dueTimestamp = periodTicks == Timeout.Infinite
                ? long.MaxValue
                : checked(currentTimestamp + periodTicks);
            return true;
        }

        public void Fire() =>
            callback(state);

        private static long GetDueTimestamp(
            long currentTimestamp,
            TimeSpan dueTime) =>
            dueTime == Timeout.InfiniteTimeSpan
                ? long.MaxValue
                : checked(
                    currentTimestamp +
                    dueTime.Ticks);

        private static long GetPeriodTicks(
            TimeSpan period) =>
            period == Timeout.InfiniteTimeSpan ||
            period == TimeSpan.Zero
                ? Timeout.Infinite
                : period.Ticks;
    }
}
