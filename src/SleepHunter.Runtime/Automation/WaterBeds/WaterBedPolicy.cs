namespace SleepHunter.Runtime.Automation.WaterBeds;

public sealed record WaterBedPolicy
{
    public static readonly TimeSpan DefaultMinimumInterval =
        TimeSpan.FromMilliseconds(500);

    public static readonly TimeSpan DefaultActionDuration =
        TimeSpan.FromMilliseconds(50);

    public WaterBedPolicy(
        int targetX,
        int targetY,
        int manaThreshold = 1000,
        int maximumXDistance = 10,
        int maximumYDistance = 10,
        TimeSpan? minimumInterval = null,
        TimeSpan? actionDuration = null)
    {
        if (targetX < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetX),
                targetX,
                "Water and bed target X coordinates cannot be negative.");
        }

        if (targetY < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetY),
                targetY,
                "Water and bed target Y coordinates cannot be negative.");
        }

        if (manaThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manaThreshold),
                manaThreshold,
                "Water and bed mana thresholds cannot be negative.");
        }

        if (maximumXDistance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumXDistance),
                maximumXDistance,
                "Maximum water and bed X distance cannot be negative.");
        }

        if (maximumYDistance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumYDistance),
                maximumYDistance,
                "Maximum water and bed Y distance cannot be negative.");
        }

        var resolvedMinimumInterval =
            minimumInterval ?? DefaultMinimumInterval;
        if (resolvedMinimumInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumInterval),
                resolvedMinimumInterval,
                "Water and bed minimum intervals must be positive.");
        }

        var resolvedActionDuration =
            actionDuration ?? DefaultActionDuration;
        if (resolvedActionDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionDuration),
                resolvedActionDuration,
                "Water and bed action durations must be positive.");
        }

        TargetX = targetX;
        TargetY = targetY;
        ManaThreshold = manaThreshold;
        MaximumXDistance = maximumXDistance;
        MaximumYDistance = maximumYDistance;
        MinimumInterval = resolvedMinimumInterval;
        ActionDuration = resolvedActionDuration;
    }

    public int TargetX { get; }

    public int TargetY { get; }

    public int ManaThreshold { get; }

    public int MaximumXDistance { get; }

    public int MaximumYDistance { get; }

    public TimeSpan MinimumInterval { get; }

    public TimeSpan ActionDuration { get; }
}
