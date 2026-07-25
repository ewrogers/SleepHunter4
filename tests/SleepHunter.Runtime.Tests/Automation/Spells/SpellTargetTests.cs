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
        var relativeArea = SpellTarget.RelativeArea(
            -1,
            2,
            innerRadius: 1,
            outerRadius: 3,
            new TargetOffset(4, -5));
        var absoluteArea = SpellTarget.AbsoluteArea(
            100,
            200,
            innerRadius: 0,
            outerRadius: 2);

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
            Assert.That(relativeArea.IsArea, Is.True);
            Assert.That(relativeArea.InnerRadius, Is.EqualTo(1));
            Assert.That(relativeArea.OuterRadius, Is.EqualTo(3));
            Assert.That(relativeArea.Offset, Is.EqualTo(new TargetOffset(4, -5)));
            Assert.That(absoluteArea.Kind, Is.EqualTo(SpellTargetKind.AbsoluteArea));
            Assert.That(
                SpellTarget.Self.WithOffset(8, 9).Offset,
                Is.EqualTo(new TargetOffset(8, 9)));
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
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.RelativeArea(0, 0, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.RelativeArea(0, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.RelativeArea(
                    0,
                    0,
                    0,
                    SpellTarget.MaximumAreaRadius + 1));
            Assert.Throws<ArgumentException>(
                () => SpellTarget.RelativeArea(0, 0, 2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellTarget.AbsoluteArea(-1, 0, 0, 1));
            Assert.Throws<InvalidOperationException>(
                () => SpellTarget.None.WithOffset(1, 1));
            Assert.Throws<InvalidOperationException>(
                () => SpellTarget.ScreenPoint(1, 1).WithOffset(1, 1));
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
