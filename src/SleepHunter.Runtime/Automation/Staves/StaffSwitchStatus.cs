namespace SleepHunter.Runtime.Automation.Staves;

public enum StaffSwitchStatus
{
    WaitingForInventory,
    ChangingWeapon,
    Succeeded,
    NoChange,
    SnapshotUnavailable,
    SelectionInvalidated,
    PanelUnavailable,
    TimedOut,
    Cancelled
}
