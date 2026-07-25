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
}
