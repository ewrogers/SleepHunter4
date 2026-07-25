namespace SleepHunter.Interop.Snapshots;

[Flags]
public enum SnapshotCaptureSections
{
    Core = 0,
    Inventory = 1 << 0,
    Equipment = 1 << 1,
    Skillbook = 1 << 2,
    Spellbook = 1 << 3,
    All = Inventory | Equipment | Skillbook | Spellbook
}
