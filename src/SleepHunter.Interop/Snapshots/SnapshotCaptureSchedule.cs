namespace SleepHunter.Interop.Snapshots;

public sealed record SnapshotCaptureSchedule
{
    public const int DefaultTimingWindowCapacity = 256;
    public const int MaximumTimingWindowCapacity = 4096;

    public static TimeSpan MinimumInterval { get; } =
        TimeSpan.FromMilliseconds(1);

    public static TimeSpan MaximumInterval { get; } =
        TimeSpan.FromMilliseconds(uint.MaxValue - 1d);

    public SnapshotCaptureSchedule(
        TimeSpan interval,
        SnapshotCaptureSections sections = SnapshotCaptureSections.Core,
        int timingWindowCapacity = DefaultTimingWindowCapacity,
        bool captureImmediately = true)
    {
        if (interval < MinimumInterval || interval > MaximumInterval)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                interval,
                $"The snapshot capture interval must be between {MinimumInterval} and {MaximumInterval}.");
        }

        if ((sections & ~SnapshotCaptureSections.All) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sections),
                sections,
                "The requested snapshot sections are not supported.");
        }

        if (timingWindowCapacity is <= 0 or > MaximumTimingWindowCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timingWindowCapacity),
                timingWindowCapacity,
                $"The timing window capacity must be between 1 and {MaximumTimingWindowCapacity}.");
        }

        Interval = interval;
        Sections = sections;
        TimingWindowCapacity = timingWindowCapacity;
        CaptureImmediately = captureImmediately;
    }

    public TimeSpan Interval { get; }

    public SnapshotCaptureSections Sections { get; }

    public int TimingWindowCapacity { get; }

    public bool CaptureImmediately { get; }
}
