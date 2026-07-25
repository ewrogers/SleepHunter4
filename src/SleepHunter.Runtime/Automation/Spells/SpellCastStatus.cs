namespace SleepHunter.Runtime.Automation.Spells;

public enum SpellCastStatus
{
    QueueEmpty,
    SnapshotUnavailable,
    Waiting,
    WaitingForHealth,
    WaitingForMana,
    CoolingDown,
    Complete,
    Unavailable,
    WaitingForStaff,
    WaitingForPanel,
    TargetUnavailable,
    Casting,
    Succeeded,
    SelectionInvalidated,
    StaffUnavailable,
    PanelUnavailable,
    Cancelled
}
