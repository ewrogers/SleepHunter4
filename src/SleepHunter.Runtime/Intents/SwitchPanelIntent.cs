using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.Runtime.Intents;

public sealed record SwitchPanelIntent : ClientActionIntent
{
    public SwitchPanelIntent(
        ClientActionId actionId,
        ClientPanel targetPanel)
        : base(actionId)
    {
        if (!Enum.IsDefined(targetPanel) ||
            targetPanel == ClientPanel.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetPanel),
                targetPanel,
                "Panel switch intents require a known target panel.");
        }

        TargetPanel = targetPanel;
    }

    public ClientPanel TargetPanel { get; }
}
