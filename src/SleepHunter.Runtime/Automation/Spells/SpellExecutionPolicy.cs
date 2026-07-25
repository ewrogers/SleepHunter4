using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellExecutionPolicy
{
    public static SpellExecutionPolicy Default { get; } = new(
        SpellCastPolicy.Default,
        PanelTransitionPolicy.Default);

    public SpellExecutionPolicy(
        SpellCastPolicy? cast = null,
        PanelTransitionPolicy? panelTransition = null)
    {
        Cast = cast ?? SpellCastPolicy.Default;
        PanelTransition = panelTransition ?? PanelTransitionPolicy.Default;
    }

    public SpellCastPolicy Cast { get; }

    public PanelTransitionPolicy PanelTransition { get; }
}
