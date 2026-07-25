namespace SleepHunter.Runtime.Automation.Flowering;

public enum FlowerReadinessStatus
{
    Ready,
    WaitingForInterval,
    WaitingForMana,
    WaitingForCondition,
    TargetUnavailable,
    LocationUnavailable,
    OutOfRange
}
