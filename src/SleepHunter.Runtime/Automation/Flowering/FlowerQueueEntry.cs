using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerQueueEntry
{
    public FlowerQueueEntry(
        FlowerQueueEntryId id,
        SpellTarget target,
        TimeSpan? interval = null,
        int? manaThreshold = null)
    {
        if (id.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "Flower queue entries require a valid identifier.");
        }

        ArgumentNullException.ThrowIfNull(target);

        if (target.Kind == SpellTargetKind.None)
        {
            throw new ArgumentException(
                "Flower queue entries require a target.",
                nameof(target));
        }

        if (interval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                interval,
                "Flower intervals cannot be negative.");
        }

        if (manaThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manaThreshold),
                manaThreshold,
                "Flower mana thresholds cannot be negative.");
        }

        if (manaThreshold is not null &&
            target.Kind != SpellTargetKind.Character)
        {
            throw new ArgumentException(
                "Flower mana thresholds require a character target.",
                nameof(manaThreshold));
        }

        if (interval is null && manaThreshold is null)
        {
            throw new ArgumentException(
                "Flower queue entries require an interval or mana threshold.");
        }

        Id = id;
        Target = target;
        Interval = interval;
        ManaThreshold = manaThreshold;
    }

    public FlowerQueueEntryId Id { get; }

    public SpellTarget Target { get; }

    public TimeSpan? Interval { get; }

    public int? ManaThreshold { get; }
}
