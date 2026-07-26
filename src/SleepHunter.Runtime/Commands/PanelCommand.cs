using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.Runtime.Commands;

public sealed record RequestPanelTransitionCommand : MacroCommand
{
    public RequestPanelTransitionCommand(
        ClientPanel targetPanel,
        PanelTransitionPolicy? policy = null)
    {
        if (!Enum.IsDefined(targetPanel) ||
            targetPanel == ClientPanel.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetPanel),
                targetPanel,
                "Panel transitions require a known target panel.");
        }

        TargetPanel = targetPanel;
        Policy = policy ?? PanelTransitionPolicy.Default;
    }

    public ClientPanel TargetPanel { get; }

    public PanelTransitionPolicy Policy { get; }
}
