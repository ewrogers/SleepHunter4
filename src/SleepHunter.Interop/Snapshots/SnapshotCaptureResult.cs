using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

public sealed record SnapshotCaptureResult
{
    public SnapshotCaptureResult(
        ClientSnapshot? snapshot,
        SnapshotQuality quality,
        SnapshotCaptureError? error,
        SnapshotCaptureMetrics metrics)
    {
        if (!Enum.IsDefined(quality))
        {
            throw new ArgumentOutOfRangeException(
                nameof(quality),
                quality,
                "The snapshot quality is not supported.");
        }

        ArgumentNullException.ThrowIfNull(metrics);

        if (snapshot is not null &&
            (quality != SnapshotQuality.Complete ||
             snapshot.Quality != SnapshotQuality.Complete ||
             error is not null ||
             snapshot.Sequence != metrics.Sequence ||
             snapshot.CaptureStartedAt != metrics.CaptureStartedAt ||
             snapshot.CaptureCompletedAt != metrics.CaptureCompletedAt))
        {
            throw new ArgumentException(
                "A published client snapshot must be complete, error-free, and match its capture metrics.",
                nameof(snapshot));
        }

        if (snapshot is null &&
            (quality == SnapshotQuality.Complete || error is null))
        {
            throw new ArgumentException(
                "A failed snapshot capture must be incomplete and report an error.",
                nameof(error));
        }

        Snapshot = snapshot;
        Quality = quality;
        Error = error;
        Metrics = metrics;
    }

    public ClientSnapshot? Snapshot { get; }

    public SnapshotQuality Quality { get; }

    public SnapshotCaptureError? Error { get; }

    public SnapshotCaptureMetrics Metrics { get; }

    public bool Succeeded => Snapshot is not null;
}
