using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Snapshots;

public sealed record SnapshotSectionMetrics
{
    public SnapshotSectionMetrics(
        SnapshotSection section,
        TimeSpan duration,
        bool succeeded,
        MemoryReadMetrics reads)
    {
        if (!Enum.IsDefined(section))
        {
            throw new ArgumentOutOfRangeException(
                nameof(section),
                section,
                "The snapshot section is not supported.");
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "A snapshot section duration cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(reads);

        Section = section;
        Duration = duration;
        Succeeded = succeeded;
        Reads = reads;
    }

    public SnapshotSection Section { get; }

    public TimeSpan Duration { get; }

    public bool Succeeded { get; }

    public MemoryReadMetrics Reads { get; }
}
