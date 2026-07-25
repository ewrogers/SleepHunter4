namespace SleepHunter.Runtime.Automation.Spells;

public enum SpellCastStatus
{
    QueueEmpty,
    SnapshotUnavailable,
    Waiting,
    WaitingForMana,
    CoolingDown,
    Complete,
    Unavailable,
    WaitingForStaff,
    WaitingForPanel,
    Casting,
    Succeeded,
    SelectionInvalidated,
    StaffUnavailable,
    PanelUnavailable,
    Cancelled
}
