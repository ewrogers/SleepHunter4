namespace SleepHunter.Interop.Snapshots;

[Flags]
public enum SnapshotCaptureSections
{
    Core = 0,
    Inventory = 1 << 0,
    Equipment = 1 << 1,
    All = Inventory | Equipment
}
