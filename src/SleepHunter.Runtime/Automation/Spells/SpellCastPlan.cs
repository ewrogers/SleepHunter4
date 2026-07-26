using System.Collections.Immutable;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellCastPlan
{
    internal SpellCastPlan(
        SpellPlanStatus status,
        SpellQueueEntry? selectedEntry,
        SpellSnapshot? selectedSpell,
        TimeSpan? castDuration,
        SpellQueueState queue,
        SpellCooldownState cooldowns,
        ImmutableArray<SpellReadiness> readiness)
    {
        Status = status;
        SelectedEntry = selectedEntry;
        SelectedSpell = selectedSpell;
        CastDuration = castDuration;
        Queue = queue;
        Cooldowns = cooldowns;
        Readiness = readiness;
    }

    public SpellPlanStatus Status { get; }

    public SpellQueueEntry? SelectedEntry { get; }

    public SpellSnapshot? SelectedSpell { get; }

    public TimeSpan? CastDuration { get; }

    public SpellQueueState Queue { get; }

    public SpellCooldownState Cooldowns { get; }

    public ImmutableArray<SpellReadiness> Readiness { get; }

    public bool HasSelection => SelectedEntry is not null;
}
