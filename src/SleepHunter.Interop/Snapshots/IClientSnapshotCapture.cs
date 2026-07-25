using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

public interface IClientSnapshotCapture
{
    ClientIdentity Client { get; }

    SnapshotCaptureResult Capture(SnapshotSequence sequence);
}
