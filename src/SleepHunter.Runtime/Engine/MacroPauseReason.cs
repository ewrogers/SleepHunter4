namespace SleepHunter.Runtime.Engine;

public enum MacroPauseReason
{
    None,
    UserRequested,
    MapChanged,
    CoordinatesChanged,
    ClientActionFailed
}
