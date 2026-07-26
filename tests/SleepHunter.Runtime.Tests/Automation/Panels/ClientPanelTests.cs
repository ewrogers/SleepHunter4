using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.Runtime.Tests.Automation.Panels;

public sealed class ClientPanelTests
{
    [TestCase(ClientPanel.Inventory, 1, true)]
    [TestCase(ClientPanel.Inventory, 12, true)]
    [TestCase(ClientPanel.Inventory, 13, false)]
    [TestCase(ClientPanel.TemuairSkills, 12, true)]
    [TestCase(ClientPanel.TemuairSkills, 13, false)]
    [TestCase(ClientPanel.MedeniaSpells, 48, true)]
    [TestCase(ClientPanel.MedeniaSpells, 49, false)]
    [TestCase(ClientPanel.WorldSkills, 78, true)]
    [TestCase(ClientPanel.WorldSkills, 79, false)]
    [TestCase(ClientPanel.WorldSpells, 90, false)]
    public void ShouldIdentifySlotsVisibleInMinimizedMode(
        ClientPanel panel,
        int slot,
        bool expectedVisible)
    {
        Assert.That(
            panel.IsSlotVisibleInMinimizedMode(slot),
            Is.EqualTo(expectedVisible));
    }
}
