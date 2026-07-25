using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellReadiness
{
    internal SpellReadiness(
        SpellQueueEntry entry,
        SpellSnapshot? spell,
        SpellReadinessStatus status,
        MacroTimestamp? readyAt)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Spell readiness status is not supported.");
        }

        if ((status == SpellReadinessStatus.Missing) != (spell is null))
        {
            throw new ArgumentException(
                "Only missing spell readiness can omit the observed spell.",
                nameof(spell));
        }

        if (status != SpellReadinessStatus.CoolingDown &&
            readyAt is not null)
        {
            throw new ArgumentException(
                "Only cooling-down readiness can expose a ready time.",
                nameof(readyAt));
        }

        Entry = entry;
        Spell = spell;
        Status = status;
        ReadyAt = readyAt;
    }

    public SpellQueueEntry Entry { get; }

    public SpellSnapshot? Spell { get; }

    public SpellReadinessStatus Status { get; }

    public MacroTimestamp? ReadyAt { get; }
}
