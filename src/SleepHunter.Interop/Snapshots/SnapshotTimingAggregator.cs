using System.Collections.Immutable;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Snapshots;

internal sealed class SnapshotTimingAggregator
{
    private readonly int capacity;
    private readonly Queue<CaptureSample> samples;

    public SnapshotTimingAggregator(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "The timing window capacity must be positive.");
        }

        this.capacity = capacity;
        samples = new Queue<CaptureSample>(capacity);
    }

    public void Record(SnapshotCaptureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (samples.Count == capacity)
        {
            samples.Dequeue();
        }

        samples.Enqueue(
            new CaptureSample(
                result.Succeeded,
                result.Error?.Failure,
                result.Metrics.Duration,
                result.Metrics.Reads,
                result.Metrics.Sections));
    }

    public SnapshotCaptureStatistics CreateStatistics()
    {
        var succeededCount = samples.Count(sample => sample.Succeeded);
        var failedCount = samples.Count - succeededCount;
        var failures = samples
            .Where(sample => sample.Failure is not null)
            .GroupBy(sample => sample.Failure!.Value)
            .ToImmutableDictionary(
                group => group.Key,
                group => group.Count());
        var sections = samples
            .SelectMany(sample => sample.Sections)
            .GroupBy(section => section.Section)
            .OrderBy(group => group.Key)
            .Select(
                group =>
                    new SnapshotSectionStatistics(
                        group.Key,
                        group.Count(section => section.Succeeded),
                        group.Count(section => !section.Succeeded),
                        CalculateDurations(
                            group.Select(section => section.Duration)),
                        SumReads(group.Select(section => section.Reads))))
            .ToImmutableArray();

        return new SnapshotCaptureStatistics(
            capacity,
            succeededCount,
            failedCount,
            CalculateDurations(samples.Select(sample => sample.Duration)),
            SumReads(samples.Select(sample => sample.Reads)),
            failures,
            sections);
    }

    private static SnapshotDurationStatistics CalculateDurations(
        IEnumerable<TimeSpan> durations)
    {
        var orderedTicks = durations
            .Select(duration => duration.Ticks)
            .Order()
            .ToArray();
        if (orderedTicks.Length == 0)
        {
            return SnapshotDurationStatistics.Empty;
        }

        var middle = orderedTicks.Length / 2;
        var medianTicks = orderedTicks.Length % 2 == 0
            ? Midpoint(orderedTicks[middle - 1], orderedTicks[middle])
            : orderedTicks[middle];
        var averageTicks = decimal.ToInt64(
            decimal.Round(
                orderedTicks.Aggregate(
                    0m,
                    (total, ticks) => total + ticks) /
                orderedTicks.Length,
                decimals: 0,
                MidpointRounding.AwayFromZero));
        var percentile95Index = Math.Max(
            0,
            (int)Math.Ceiling(orderedTicks.Length * 0.95) - 1);
        return new SnapshotDurationStatistics(
            orderedTicks.Length,
            TimeSpan.FromTicks(orderedTicks[0]),
            TimeSpan.FromTicks(averageTicks),
            TimeSpan.FromTicks(medianTicks),
            TimeSpan.FromTicks(orderedTicks[percentile95Index]),
            TimeSpan.FromTicks(orderedTicks[^1]));
    }

    private static MemoryReadMetrics SumReads(
        IEnumerable<MemoryReadMetrics> reads)
    {
        var requestCount = 0;
        var transportReadCount = 0;
        var failedReadCount = 0;
        long requestedBytes = 0;
        long bytesRead = 0;

        foreach (var metrics in reads)
        {
            requestCount = checked(requestCount + metrics.RequestCount);
            transportReadCount = checked(
                transportReadCount + metrics.TransportReadCount);
            failedReadCount = checked(
                failedReadCount + metrics.FailedReadCount);
            requestedBytes = checked(
                requestedBytes + metrics.RequestedBytes);
            bytesRead = checked(bytesRead + metrics.BytesRead);
        }

        return new MemoryReadMetrics(
            requestCount,
            transportReadCount,
            failedReadCount,
            requestedBytes,
            bytesRead);
    }

    private static long Midpoint(long left, long right) =>
        left + ((right - left) / 2);

    private sealed record CaptureSample(
        bool Succeeded,
        SnapshotCaptureFailure? Failure,
        TimeSpan Duration,
        MemoryReadMetrics Reads,
        ImmutableArray<SnapshotSectionMetrics> Sections);
}
