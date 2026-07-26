namespace SleepHunter.Runtime.Engine;

public enum MacroStopReason
{
    None,
    UserRequested,
    RuntimeFailure,
    ClientLoggedOut,
    MapChanged,
    CoordinatesChanged
}
