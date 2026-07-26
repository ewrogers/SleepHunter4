namespace SleepHunter.Runtime.Automation.Panels;

public enum ClientPanel
{
    Unknown,
    Inventory,
    TemuairSpells,
    MedeniaSpells,
    TemuairSkills,
    MedeniaSkills,
    Chat,
    ChatHistory,
    Stats,
    Modifiers,
    WorldSkills,
    WorldSpells
}

public static class ClientPanelExtensions
{
    public static bool IsEquivalentTo(
        this ClientPanel panel,
        ClientPanel target) =>
        panel == target ||
        (panel is ClientPanel.WorldSkills or ClientPanel.WorldSpells &&
         target is ClientPanel.WorldSkills or ClientPanel.WorldSpells);

    public static bool IsSlotVisibleInMinimizedMode(
        this ClientPanel panel,
        int slot)
    {
        if (slot <= 0)
        {
            return false;
        }

        var (panelCapacity, rowSize) = panel switch
        {
            ClientPanel.Inventory => (60, 12),
            ClientPanel.TemuairSkills or
                ClientPanel.MedeniaSkills or
                ClientPanel.TemuairSpells or
                ClientPanel.MedeniaSpells => (36, 12),
            ClientPanel.WorldSkills or
                ClientPanel.WorldSpells => (18, 6),
            _ => (0, 0)
        };
        if (panelCapacity == 0)
        {
            return false;
        }

        var relativeSlot = ((slot - 1) % panelCapacity) + 1;
        return relativeSlot <= rowSize;
    }
}
