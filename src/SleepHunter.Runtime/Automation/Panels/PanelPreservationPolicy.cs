namespace SleepHunter.Runtime.Automation.Panels;

public sealed record PanelPreservationPolicy
{
    public static PanelPreservationPolicy Disabled { get; } = new();

    public static PanelPreservationPolicy EnabledDefault { get; } = new(
        enabled: true);

    public PanelPreservationPolicy(
        bool enabled = false,
        PanelTransitionPolicy? transition = null)
    {
        Enabled = enabled;
        Transition = transition ?? PanelTransitionPolicy.Default;
    }

    public bool Enabled { get; }

    public PanelTransitionPolicy Transition { get; }
}
