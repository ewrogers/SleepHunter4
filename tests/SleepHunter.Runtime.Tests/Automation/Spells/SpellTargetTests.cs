using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Intents;

namespace SleepHunter.Runtime.Tests.Automation.Spells;

public sealed class SpellTargetTests
{
    [Test]
    public void ShouldCreateImmutableSemanticTargets()
    {
        var character = SpellTarget.Character("  Alt  ");
        var relative = SpellTarget.RelativeTile(-2, 3);
        var absolute = SpellTarget.AbsoluteTile(100, 200);
        var screen = SpellTarget.ScreenPoint(315, 160);

        Assert.Multiple(() =>
        {
            Assert.That(SpellTarget.None.Kind, Is.EqualTo(SpellTargetKind.None));
            Assert.That(SpellTarget.Self.Kind, Is.EqualTo(SpellTargetKind.Self));
            Assert.That(
                character,
                Is.EqualTo(SpellTarget.Character("Alt")));
            Assert.That(character.CharacterName, Is.EqualTo("Alt"));
            Assert.That(relative.X, Is.EqualTo(-2));
            Assert.That(relative.Y, Is.EqualTo(3));
            Assert.That(absolute.Kind, Is.EqualTo(SpellTargetKind.AbsoluteTile));
            Assert.That(screen.Kind, Is.EqualTo(SpellTargetKind.ScreenPoint));
        });
    }

    [Test]
    public void ShouldPreserveTargetOnQueueEntry()
    {
        var target = SpellTarget.Self;
        var entry = new SpellQueueEntry(
            new SpellQueueEntryId(1),
            "spell",
            targetLevel: 10,
            target);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Target, Is.EqualTo(target));
            Assert.That(entry.TargetLevel, Is.EqualTo(10));
            Assert.That(
                new SpellQueueEntry(new SpellQueueEntryId(2), "spell").Target,
                Is.EqualTo(SpellTarget.None));
        });
    }

    [Test]
    public void ShouldValidateTargetAndCastIntentValues()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => SpellTarget.Character(" "));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.AbsoluteTile(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.AbsoluteTile(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.ScreenPoint(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.ScreenPoint(0, -1));
            Assert.Throws<ArgumentException>(
                () => _ = new CastSpellIntent(
                    new ClientActionId(1),
                    " ",
                    slot: 1,
                    ClientPanel.TemuairSpells,
                    SpellTarget.None));
            Assert.Throws<ArgumentException>(
                () => _ = new CastSpellIntent(
                    new ClientActionId(1),
                    "spell",
                    slot: 1,
                    ClientPanel.MedeniaSpells,
                    SpellTarget.None));
            Assert.Throws<ArgumentNullException>(
                () => _ = new CastSpellIntent(
                    new ClientActionId(1),
                    "spell",
                    slot: 1,
                    ClientPanel.TemuairSpells,
                    target: null!));
        });
    }
}
