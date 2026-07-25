namespace SleepHunter.Runtime.Automation;

public sealed record ObservationChangePolicy
{
    public static ObservationChangePolicy Default { get; } = new();

    public ObservationChangePolicy(
        ObservationChangeAction mapChange =
            ObservationChangeAction.Stop,
        ObservationChangeAction coordinateChange =
            ObservationChangeAction.Continue)
    {
        if (!Enum.IsDefined(mapChange))
        {
            throw new ArgumentOutOfRangeException(
                nameof(mapChange),
                mapChange,
                "The map-change action is not supported.");
        }

        if (!Enum.IsDefined(coordinateChange))
        {
            throw new ArgumentOutOfRangeException(
                nameof(coordinateChange),
                coordinateChange,
                "The coordinate-change action is not supported.");
        }

        MapChange = mapChange;
        CoordinateChange = coordinateChange;
    }

    public ObservationChangeAction MapChange { get; }

    public ObservationChangeAction CoordinateChange { get; }
}
