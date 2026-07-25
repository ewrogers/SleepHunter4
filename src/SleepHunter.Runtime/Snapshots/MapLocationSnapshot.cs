namespace SleepHunter.Runtime.Snapshots;

public sealed record MapLocationSnapshot
{
    public MapLocationSnapshot(
        int mapNumber,
        string mapName,
        int x,
        int y)
    {
        if (mapNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mapNumber),
                mapNumber,
                "Map numbers must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(mapName);

        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                x,
                "Map X coordinates cannot be negative.");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(y),
                y,
                "Map Y coordinates cannot be negative.");
        }

        MapNumber = mapNumber;
        MapName = mapName.Trim();
        X = x;
        Y = y;
    }

    public int MapNumber { get; }

    public string MapName { get; }

    public int X { get; }

    public int Y { get; }

    public bool IsWithinRange(
        MapLocationSnapshot other,
        int maximumXDistance = 10,
        int maximumYDistance = 10)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (maximumXDistance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumXDistance),
                maximumXDistance,
                "Maximum X distance cannot be negative.");
        }

        if (maximumYDistance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumYDistance),
                maximumYDistance,
                "Maximum Y distance cannot be negative.");
        }

        return MapNumber == other.MapNumber &&
               string.Equals(
                   MapName,
                   other.MapName,
                   StringComparison.Ordinal) &&
               Math.Abs(X - other.X) <= maximumXDistance &&
               Math.Abs(Y - other.Y) <= maximumYDistance;
    }
}
