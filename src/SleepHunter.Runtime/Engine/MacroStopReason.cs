namespace SleepHunter.Runtime.Engine;

public enum MacroStopReason
{
    None,
    UserRequested,
    ClientLoggedOut,
    MapChanged,
    CoordinatesChanged
}
