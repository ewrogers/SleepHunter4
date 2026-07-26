namespace SleepHunter.Runtime.Automation.Flowering;

public enum FlowerClientReadinessStatus
{
    Ready,
    SourceClient,
    MacroStopped,
    NotWaitingForMana,
    LoggedOut,
    LocationUnavailable,
    OutOfRange
}
