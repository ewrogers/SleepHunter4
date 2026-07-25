using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class SpellbookSnapshotTests
{
    private static readonly int[] ExpectedSortedSlots = [1, 37, 73];

    [Test]
    public void ShouldNormalizeAndLocateSpellsByName()
    {
        var world = CreateSpell(" world ", slot: 73);
        var temuair = CreateSpell("temuair", slot: 1);
        var medenia = CreateSpell("medenia", slot: 37);
        var spellbook = new SpellbookSnapshot(
        [
            world,
            medenia,
            temuair
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(
                spellbook.Spells.Select(spell => spell.Slot),
                Is.EqualTo(ExpectedSortedSlots));
            Assert.That(spellbook.Find(" WORLD "), Is.EqualTo(world));
        });
    }

    [TestCase(1, ClientPanel.TemuairSpells)]
    [TestCase(36, ClientPanel.TemuairSpells)]
    [TestCase(37, ClientPanel.MedeniaSpells)]
    [TestCase(72, ClientPanel.MedeniaSpells)]
    [TestCase(73, ClientPanel.WorldSpells)]
    [TestCase(SpellSnapshot.MaximumSlot, ClientPanel.WorldSpells)]
    public void ShouldMapAbsoluteSlotToSpellPanel(
        int slot,
        ClientPanel expectedPanel)
    {
        var spell = CreateSpell("spell", slot);

        Assert.That(spell.Panel, Is.EqualTo(expectedPanel));
    }

    [Test]
    public void ShouldRejectInvalidSpellValues()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = CreateSpell(" "));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = CreateSpell("spell", slot: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = CreateSpell(
                    "spell",
                    slot: SpellSnapshot.MaximumSlot + 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = CreateSpell("spell", currentLevel: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = CreateSpell("spell", maximumLevel: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = CreateSpell("spell", castLines: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = CreateSpell("spell", manaCost: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = CreateSpell(
                    "spell",
                    cooldown: TimeSpan.FromTicks(-1)));
        });
    }

    [Test]
    public void ShouldRejectDuplicateSpellSlotsAndNames()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = new SpellbookSnapshot(
                [
                    CreateSpell("first", slot: 1),
                    CreateSpell("second", slot: 1)
                ]));
            Assert.Throws<ArgumentException>(
                () => _ = new SpellbookSnapshot(
                [
                    CreateSpell("same", slot: 1),
                    CreateSpell(" SAME ", slot: 2)
                ]));
        });
    }

    [Test]
    public void ShouldCompareIndependentSpellbooksByValue()
    {
        var first = new SpellbookSnapshot(
        [
            CreateSpell("first", slot: 1),
            CreateSpell("second", slot: 37)
        ]);
        var second = new SpellbookSnapshot(
        [
            CreateSpell("second", slot: 37),
            CreateSpell("first", slot: 1)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        });
    }

    private static SpellSnapshot CreateSpell(
        string name,
        int slot = 1,
        int currentLevel = 0,
        int maximumLevel = 100,
        int castLines = 1,
        int manaCost = 0,
        TimeSpan? cooldown = null) =>
        new(
            name,
            slot,
            currentLevel,
            maximumLevel,
            castLines,
            manaCost,
            cooldown ?? TimeSpan.Zero);
}
