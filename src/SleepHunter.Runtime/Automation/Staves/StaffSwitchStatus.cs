namespace SleepHunter.Runtime.Automation.Staves;

public enum StaffSwitchStatus
{
    WaitingForInventory,
    ExpandingInterface,
    ChangingInventoryMode,
    ChangingWeapon,
    Succeeded,
    NoChange,
    SnapshotUnavailable,
    SelectionInvalidated,
    PanelUnavailable,
    TimedOut,
    IssueFailed,
    Cancelled
}
