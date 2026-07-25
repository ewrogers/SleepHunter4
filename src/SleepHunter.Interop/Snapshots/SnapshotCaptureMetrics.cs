using System.Collections.Immutable;
using SleepHunter.Interop.Memory;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Snapshots;

public sealed record SnapshotCaptureMetrics
{
    public SnapshotCaptureMetrics(
        SnapshotSequence sequence,
        MacroTimestamp captureStartedAt,
        MacroTimestamp captureCompletedAt,
        ImmutableArray<SnapshotSectionMetrics> sections,
        MemoryReadMetrics reads)
    {
        if (sequence.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "Snapshot sequences must be positive.");
        }

        if (captureCompletedAt < captureStartedAt)
        {
            throw new ArgumentException(
                "Snapshot capture metrics cannot complete before they start.",
                nameof(captureCompletedAt));
        }

        ArgumentNullException.ThrowIfNull(reads);

        Sequence = sequence;
        CaptureStartedAt = captureStartedAt;
        CaptureCompletedAt = captureCompletedAt;
        Sections = sections.IsDefault
            ? ImmutableArray<SnapshotSectionMetrics>.Empty
            : sections;
        Reads = reads;
    }

    public SnapshotSequence Sequence { get; }

    public MacroTimestamp CaptureStartedAt { get; }

    public MacroTimestamp CaptureCompletedAt { get; }

    public TimeSpan Duration =>
        CaptureCompletedAt.Elapsed - CaptureStartedAt.Elapsed;

    public ImmutableArray<SnapshotSectionMetrics> Sections { get; }

    public MemoryReadMetrics Reads { get; }
}
