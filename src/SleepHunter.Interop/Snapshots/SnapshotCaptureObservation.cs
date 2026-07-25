namespace SleepHunter.Interop.Snapshots;

public sealed record SnapshotCaptureObservation
{
    public SnapshotCaptureObservation(
        SnapshotCaptureResult result,
        SnapshotCaptureStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(statistics);

        if (statistics.SampleCount == 0)
        {
            throw new ArgumentException(
                "Capture observations require at least one timing sample.",
                nameof(statistics));
        }

        var includesResult = result.Succeeded
            ? statistics.SucceededCount > 0
            : result.Error is { } error &&
              statistics.FailedCount > 0 &&
              statistics.Failures.ContainsKey(error.Failure);
        if (!includesResult)
        {
            throw new ArgumentException(
                "Capture observation statistics must include the observed result.",
                nameof(statistics));
        }

        Result = result;
        Statistics = statistics;
    }

    public SnapshotCaptureResult Result { get; }

    public SnapshotCaptureStatistics Statistics { get; }
}
