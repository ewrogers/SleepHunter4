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
    WaitingForPanel,
    Casting,
    Succeeded,
    SelectionInvalidated,
    PanelUnavailable,
    Cancelled
}
