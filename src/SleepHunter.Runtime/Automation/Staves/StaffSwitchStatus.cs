namespace SleepHunter.Runtime.Automation.Staves;

public enum StaffSwitchStatus
{
    WaitingForInventory,
    ChangingInventoryMode,
    ChangingWeapon,
    Succeeded,
    NoChange,
    SnapshotUnavailable,
    SelectionInvalidated,
    PanelUnavailable,
    TimedOut,
    Cancelled
}
