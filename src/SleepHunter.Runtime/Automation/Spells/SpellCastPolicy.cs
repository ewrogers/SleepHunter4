namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellCastPolicy
{
    public static SpellCastPolicy Default { get; } = new(
        requireMana: true,
        SpellCastTimingPolicy.Default);

    public SpellCastPolicy(
        bool requireMana,
        SpellCastTimingPolicy? timing = null)
    {
        RequireMana = requireMana;
        Timing = timing ?? SpellCastTimingPolicy.Default;
    }

    public bool RequireMana { get; }

    public SpellCastTimingPolicy Timing { get; }
}
