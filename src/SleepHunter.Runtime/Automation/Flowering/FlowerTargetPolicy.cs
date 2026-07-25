namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerTargetPolicy
{
    public static FlowerTargetPolicy Default { get; } = new();

    public FlowerTargetPolicy(
        bool autoFlowerWaitingCharacters = false,
        bool prioritizeAlternateCharacters = true,
        int maximumXDistance = 10,
        int maximumYDistance = 10)
    {
        if (maximumXDistance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumXDistance),
                maximumXDistance,
                "Maximum flower X distance cannot be negative.");
        }

        if (maximumYDistance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumYDistance),
                maximumYDistance,
                "Maximum flower Y distance cannot be negative.");
        }

        AutoFlowerWaitingCharacters = autoFlowerWaitingCharacters;
        PrioritizeAlternateCharacters = prioritizeAlternateCharacters;
        MaximumXDistance = maximumXDistance;
        MaximumYDistance = maximumYDistance;
    }

    public bool AutoFlowerWaitingCharacters { get; }

    public bool PrioritizeAlternateCharacters { get; }

    public int MaximumXDistance { get; }

    public int MaximumYDistance { get; }
}
