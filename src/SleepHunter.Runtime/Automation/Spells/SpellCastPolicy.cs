namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellCastPolicy
{
    public static SpellCastPolicy Default { get; } = new(
        requireMana: true,
        SpellCastTimingPolicy.Default);

    public SpellCastPolicy(
        bool requireMana,
        SpellCastTimingPolicy? timing = null,
        bool skipCoolingDownSpells = true)
    {
        RequireMana = requireMana;
        Timing = timing ?? SpellCastTimingPolicy.Default;
        SkipCoolingDownSpells = skipCoolingDownSpells;
    }

    public bool RequireMana { get; }

    public SpellCastTimingPolicy Timing { get; }

    public bool SkipCoolingDownSpells { get; }
}
