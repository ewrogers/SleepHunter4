namespace SleepHunter.Runtime.Automation.Skills;

public enum SkillUseStatus
{
    QueueEmpty,
    SnapshotUnavailable,
    Waiting,
    WaitingForHealth,
    WaitingForMana,
    CoolingDown,
    Unavailable,
    WaitingForDisarm,
    WaitingForPanel,
    Using,
    Assailing,
    Succeeded,
    SelectionInvalidated,
    DisarmUnavailable,
    PanelUnavailable,
    IssueFailed,
    Cancelled
}
