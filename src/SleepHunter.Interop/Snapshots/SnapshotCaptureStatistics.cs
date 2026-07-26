using System.Collections.Immutable;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Snapshots;

public sealed record SnapshotDurationStatistics
{
    public SnapshotDurationStatistics(
        int sampleCount,
        TimeSpan minimum,
        TimeSpan average,
        TimeSpan median,
        TimeSpan percentile95,
        TimeSpan maximum)
    {
        if (sampleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleCount),
                sampleCount,
                "The duration sample count cannot be negative.");
        }

        if (minimum < TimeSpan.Zero ||
            average < TimeSpan.Zero ||
            median < TimeSpan.Zero ||
            percentile95 < TimeSpan.Zero ||
            maximum < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(median),
                "Capture durations cannot be negative.");
        }

        if (sampleCount == 0 &&
            (minimum != TimeSpan.Zero ||
             average != TimeSpan.Zero ||
             median != TimeSpan.Zero ||
             percentile95 != TimeSpan.Zero ||
             maximum != TimeSpan.Zero))
        {
            throw new ArgumentException(
                "Empty duration statistics must contain zero values.");
        }

        if (minimum > average ||
            average > maximum ||
            minimum > median ||
            median > percentile95 ||
            percentile95 > maximum)
        {
            throw new ArgumentException(
                "Duration statistics must remain within the observed minimum and maximum, with ordered percentile values.");
        }

        SampleCount = sampleCount;
        Minimum = minimum;
        Average = average;
        Median = median;
        Percentile95 = percentile95;
        Maximum = maximum;
    }

    public static SnapshotDurationStatistics Empty { get; } = new(
        sampleCount: 0,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero);

    public int SampleCount { get; }

    public TimeSpan Minimum { get; }

    public TimeSpan Average { get; }

    public TimeSpan Median { get; }

    public TimeSpan Percentile95 { get; }

    public TimeSpan Maximum { get; }
}

public sealed record SnapshotSectionStatistics
{
    public SnapshotSectionStatistics(
        SnapshotSection section,
        int succeededCount,
        int failedCount,
        SnapshotDurationStatistics duration,
        MemoryReadMetrics reads)
    {
        if (!Enum.IsDefined(section))
        {
            throw new ArgumentOutOfRangeException(
                nameof(section),
                section,
                "The snapshot section is not supported.");
        }

        if (succeededCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(succeededCount),
                succeededCount,
                "The successful section count cannot be negative.");
        }

        if (failedCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedCount),
                failedCount,
                "The failed section count cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(duration);
        ArgumentNullException.ThrowIfNull(reads);

        if (duration.SampleCount != succeededCount + failedCount)
        {
            throw new ArgumentException(
                "Section duration samples must match the section result counts.",
                nameof(duration));
        }

        Section = section;
        SucceededCount = succeededCount;
        FailedCount = failedCount;
        Duration = duration;
        Reads = reads;
    }

    public SnapshotSection Section { get; }

    public int SucceededCount { get; }

    public int FailedCount { get; }

    public SnapshotDurationStatistics Duration { get; }

    public MemoryReadMetrics Reads { get; }
}

public sealed record SnapshotCaptureStatistics
{
    public SnapshotCaptureStatistics(
        int windowCapacity,
        int succeededCount,
        int failedCount,
        SnapshotDurationStatistics duration,
        MemoryReadMetrics reads,
        ImmutableDictionary<SnapshotCaptureFailure, int> failures,
        ImmutableArray<SnapshotSectionStatistics> sections)
    {
        if (windowCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowCapacity),
                windowCapacity,
                "The timing window capacity must be positive.");
        }

        if (succeededCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(succeededCount),
                succeededCount,
                "The successful capture count cannot be negative.");
        }

        if (failedCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedCount),
                failedCount,
                "The failed capture count cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(duration);
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(failures);

        var normalizedSections = sections.IsDefault
            ? ImmutableArray<SnapshotSectionStatistics>.Empty
            : sections;
        if (normalizedSections.Any(section => section is null))
        {
            throw new ArgumentException(
                "Capture statistics cannot contain null section statistics.",
                nameof(sections));
        }

        if (failures.Any(
                failure =>
                    !Enum.IsDefined(failure.Key) ||
                    failure.Value <= 0))
        {
            throw new ArgumentException(
                "Capture failure categories must be supported and have positive counts.",
                nameof(failures));
        }

        if (duration.SampleCount != succeededCount + failedCount)
        {
            throw new ArgumentException(
                "Capture duration samples must match the capture result counts.",
                nameof(duration));
        }

        if (duration.SampleCount > windowCapacity)
        {
            throw new ArgumentException(
                "Capture samples cannot exceed the timing window capacity.",
                nameof(duration));
        }

        if (failures.Values.Sum() != failedCount)
        {
            throw new ArgumentException(
                "Capture failure categories must match the failed result count.",
                nameof(failures));
        }

        WindowCapacity = windowCapacity;
        SucceededCount = succeededCount;
        FailedCount = failedCount;
        Duration = duration;
        Reads = reads;
        Failures = failures;
        Sections = normalizedSections;
    }

    public int WindowCapacity { get; }

    public int SampleCount => SucceededCount + FailedCount;

    public int SucceededCount { get; }

    public int FailedCount { get; }

    public SnapshotDurationStatistics Duration { get; }

    public MemoryReadMetrics Reads { get; }

    public ImmutableDictionary<SnapshotCaptureFailure, int> Failures { get; }

    public ImmutableArray<SnapshotSectionStatistics> Sections { get; }

    public static SnapshotCaptureStatistics Empty(int windowCapacity) =>
        new(
            windowCapacity,
            succeededCount: 0,
            failedCount: 0,
            SnapshotDurationStatistics.Empty,
            EmptyReads(),
            ImmutableDictionary<SnapshotCaptureFailure, int>.Empty,
            ImmutableArray<SnapshotSectionStatistics>.Empty);

    private static MemoryReadMetrics EmptyReads() =>
        new(
            RequestCount: 0,
            TransportReadCount: 0,
            FailedReadCount: 0,
            RequestedBytes: 0,
            BytesRead: 0);
}
