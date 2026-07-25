namespace SleepHunter.Runtime.Automation.Flowering;

public enum FlowerStatus
{
    Idle,
    WaitingForTarget,
    SnapshotUnavailable,
    SpellUnavailable,
    WaitingForMana,
    CoolingDown,
    WaitingForStaff,
    WaitingForPanel,
    Casting,
    Succeeded,
    SelectionInvalidated,
    StaffUnavailable,
    PanelUnavailable,
    Cancelled
}
