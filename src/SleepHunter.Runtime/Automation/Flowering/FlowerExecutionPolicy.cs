using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerExecutionPolicy
{
    public static FlowerExecutionPolicy Default { get; } = new();

    public FlowerExecutionPolicy(
        FlowerTargetPolicy? target = null,
        SpellExecutionPolicy? spell = null,
        bool useVineyard = false,
        bool restoreMana = false,
        bool restoreManaOnDemand = false,
        int manaRestorationThreshold = 0,
        int? minimumManaBeforePlant = null)
    {
        if (manaRestorationThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manaRestorationThreshold),
                manaRestorationThreshold,
                "Mana restoration thresholds cannot be negative.");
        }

        if (minimumManaBeforePlant < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumManaBeforePlant),
                minimumManaBeforePlant,
                "Minimum flower mana cannot be negative.");
        }

        Target = target ?? FlowerTargetPolicy.Default;
        Spell = spell ?? SpellExecutionPolicy.Default;
        UseVineyard = useVineyard;
        RestoreMana = restoreMana;
        RestoreManaOnDemand = restoreManaOnDemand;
        ManaRestorationThreshold = manaRestorationThreshold;
        MinimumManaBeforePlant = minimumManaBeforePlant;
    }

    public FlowerTargetPolicy Target { get; }

    public SpellExecutionPolicy Spell { get; }

    public bool UseVineyard { get; }

    public bool RestoreMana { get; }

    public bool RestoreManaOnDemand { get; }

    public int ManaRestorationThreshold { get; }

    public int? MinimumManaBeforePlant { get; }
}
