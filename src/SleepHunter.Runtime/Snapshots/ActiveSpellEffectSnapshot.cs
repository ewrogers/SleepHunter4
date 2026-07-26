namespace SleepHunter.Runtime.Snapshots;

public sealed record ActiveSpellEffectSnapshot
{
    public const int MaximumSlot = 10;

    public ActiveSpellEffectSnapshot(
        int slot,
        ushort icon,
        SpellEffectDurationStage durationStage)
    {
        if (slot is <= 0 or > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Spell effect slots must be between 1 and {MaximumSlot}.");
        }

        if (!Enum.IsDefined(durationStage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationStage),
                durationStage,
                "The spell effect duration stage is not supported.");
        }

        Slot = slot;
        Icon = icon;
        DurationStage = durationStage;
    }

    public int Slot { get; }

    public ushort Icon { get; }

    public SpellEffectDurationStage DurationStage { get; }
}
