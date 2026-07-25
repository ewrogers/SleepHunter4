using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed record MacroViewSnapshot(
    long Revision,
    MacroLifecycle Lifecycle,
    MacroStopReason StopReason,
    SnapshotSequence? LatestSnapshotSequence,
    ClientPresence Presence,
    MacroTimestamp? LastTransitionAt)
{
    internal static MacroViewSnapshot FromState(MacroState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new MacroViewSnapshot(
            state.Revision,
            state.Lifecycle,
            state.StopReason,
            state.LatestSnapshot?.Sequence,
            state.LatestSnapshot?.Presence ?? ClientPresence.Unknown,
            state.LastTransitionAt);
    }
}
