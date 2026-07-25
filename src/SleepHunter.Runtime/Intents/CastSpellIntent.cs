using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Intents;

public sealed record CastSpellIntent : ClientActionIntent
{
    public CastSpellIntent(
        ClientActionId actionId,
        string spellName,
        int slot,
        ClientPanel panel,
        SpellTarget target)
        : base(actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spellName);
        ArgumentNullException.ThrowIfNull(target);

        var expectedPanel = SpellSnapshot.GetPanelForSlot(slot);
        if (panel != expectedPanel)
        {
            throw new ArgumentException(
                "The spell panel must match the absolute spell slot.",
                nameof(panel));
        }

        SpellName = spellName.Trim();
        Slot = slot;
        Panel = panel;
        Target = target;
    }

    public string SpellName { get; }

    public int Slot { get; }

    public ClientPanel Panel { get; }

    public SpellTarget Target { get; }
}
