using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Persistence.Configuration;

public sealed record FlowerOptions
{
    public static FlowerOptions Default { get; } = new();

    public FlowerOptions(
        bool useVineyard = false,
        bool flowerAlternateCharacters = false,
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

        UseVineyard = useVineyard;
        FlowerAlternateCharacters = flowerAlternateCharacters;
        PrioritizeAlternateCharacters = prioritizeAlternateCharacters;
        MaximumXDistance = maximumXDistance;
        MaximumYDistance = maximumYDistance;
    }

    public bool UseVineyard { get; }

    public bool FlowerAlternateCharacters { get; }

    public bool PrioritizeAlternateCharacters { get; }

    public int MaximumXDistance { get; }

    public int MaximumYDistance { get; }

    public FlowerExecutionPolicy CreatePolicy(
        SpellExecutionPolicy? spell = null) =>
        new(
            new FlowerTargetPolicy(
                FlowerAlternateCharacters,
                PrioritizeAlternateCharacters,
                MaximumXDistance,
                MaximumYDistance),
            spell,
            useVineyard: UseVineyard);
}
