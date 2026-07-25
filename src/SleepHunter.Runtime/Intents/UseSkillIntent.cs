using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Intents;

public sealed record UseSkillIntent : ClientActionIntent
{
    public UseSkillIntent(
        ClientActionId actionId,
        string skillName,
        int slot,
        ClientPanel panel)
        : base(actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);

        var expectedPanel = SkillSnapshot.GetPanelForSlot(slot);
        if (panel != expectedPanel)
        {
            throw new ArgumentException(
                "The skill panel must match the absolute skill slot.",
                nameof(panel));
        }

        SkillName = skillName.Trim();
        Slot = slot;
        Panel = panel;
    }

    public string SkillName { get; }

    public int Slot { get; }

    public ClientPanel Panel { get; }
}
