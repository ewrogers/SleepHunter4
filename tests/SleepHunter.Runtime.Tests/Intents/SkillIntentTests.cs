using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Intents;

namespace SleepHunter.Runtime.Tests.Intents;

public sealed class SkillIntentTests
{
    [Test]
    public void ShouldValidateSkillPanelAgainstAbsoluteSlot()
    {
        var intent = new UseSkillIntent(
            new ClientActionId(1),
            " skill ",
            slot: 37,
            ClientPanel.MedeniaSkills);

        Assert.Multiple(() =>
        {
            Assert.That(intent.SkillName, Is.EqualTo("skill"));
            Assert.That(intent.Slot, Is.EqualTo(37));
            Assert.Throws<ArgumentException>(
                () => _ = new UseSkillIntent(
                    new ClientActionId(2),
                    "skill",
                    slot: 37,
                    ClientPanel.TemuairSkills));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new UseSkillIntent(
                    new ClientActionId(3),
                    "skill",
                    slot: 0,
                    ClientPanel.TemuairSkills));
        });
    }

    [Test]
    public void ShouldNormalizeAssailSkillName()
    {
        var intent = new AssailIntent(
            new ClientActionId(1),
            " assail ");

        Assert.That(intent.SkillName, Is.EqualTo("assail"));
    }
}
