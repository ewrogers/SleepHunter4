namespace SleepHunter.Runtime.Snapshots;

public sealed record MapLocationSnapshot
{
    public MapLocationSnapshot(
        int mapNumber,
        string mapName,
        int x,
        int y,
        int width = 0,
        int height = 0,
        uint flags = 0,
        byte weather = 0,
        bool isTransferActive = false)
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

        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "Map width cannot be negative.");
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "Map height cannot be negative.");
        }

        if (width > 0 && x >= width)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                x,
                "Map X coordinates must be within the observed width.");
        }

        if (height > 0 && y >= height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(y),
                y,
                "Map Y coordinates must be within the observed height.");
        }

        MapNumber = mapNumber;
        MapName = mapName.Trim();
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Flags = flags;
        Weather = weather;
        IsTransferActive = isTransferActive;
    }

    public int MapNumber { get; }

    public string MapName { get; }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public uint Flags { get; }

    public byte Weather { get; }

    public bool IsTransferActive { get; }

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
