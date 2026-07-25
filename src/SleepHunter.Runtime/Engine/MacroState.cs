using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed record MacroState
{
    public static MacroState Initial { get; } = new(
        revision: 0,
        MacroLifecycle.Stopped,
        MacroStopReason.None,
        latestSnapshot: null,
        lastTransitionAt: null);

    internal MacroState(
        long revision,
        MacroLifecycle lifecycle,
        MacroStopReason stopReason,
        ClientSnapshot? latestSnapshot,
        MacroTimestamp? lastTransitionAt)
    {
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revision),
                revision,
                "State revisions cannot be negative.");
        }

        if (lifecycle != MacroLifecycle.Stopped &&
            stopReason != MacroStopReason.None)
        {
            throw new ArgumentException(
                "Only stopped macro state can have a stop reason.",
                nameof(stopReason));
        }

        Revision = revision;
        Lifecycle = lifecycle;
        StopReason = stopReason;
        LatestSnapshot = latestSnapshot;
        LastTransitionAt = lastTransitionAt;
    }

    public long Revision { get; }

    public MacroLifecycle Lifecycle { get; }

    public MacroStopReason StopReason { get; }

    public ClientSnapshot? LatestSnapshot { get; }

    public MacroTimestamp? LastTransitionAt { get; }
}
