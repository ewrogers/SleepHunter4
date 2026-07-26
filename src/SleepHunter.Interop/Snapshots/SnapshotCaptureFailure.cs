namespace SleepHunter.Interop.Snapshots;

public enum SnapshotCaptureFailure
{
    CaptureAlreadyInProgress,
    MappingReadFailed,
    InvalidValue,
    LocationTransition,
    StateChanged,
    OwnershipChanged
}
