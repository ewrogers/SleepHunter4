using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellPlanningRequest
{
    public SpellPlanningRequest(
        SpellQueueState queue,
        VitalsSnapshot? vitals,
        SpellbookSnapshot? spellbook,
        SpellCooldownState? cooldowns,
        MacroTimestamp currentTime,
        SpellCastPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(queue);

        Queue = queue;
        Vitals = vitals;
        Spellbook = spellbook;
        Cooldowns = cooldowns ?? SpellCooldownState.Empty;
        CurrentTime = currentTime;
        Policy = policy ?? SpellCastPolicy.Default;
    }

    public SpellQueueState Queue { get; }

    public VitalsSnapshot? Vitals { get; }

    public SpellbookSnapshot? Spellbook { get; }

    public SpellCooldownState Cooldowns { get; }

    public MacroTimestamp CurrentTime { get; }

    public SpellCastPolicy Policy { get; }
}
