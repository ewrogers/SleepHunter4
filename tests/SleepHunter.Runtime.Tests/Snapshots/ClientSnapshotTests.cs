using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class ClientSnapshotTests
{
    [Test]
    public void ShouldRejectNonPositiveSequence()
    {
        Assert.That(
            () => new SnapshotSequence(0),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ShouldRejectDefaultSequence()
    {
        var client = new ClientIdentity("client");

        Assert.That(
            () => new ClientSnapshot(
                default,
                MacroTimestamp.Zero,
                MacroTimestamp.Zero,
                client,
                SnapshotQuality.Complete,
                ClientPresence.InWorld),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ShouldRejectCaptureThatCompletesBeforeItStarts()
    {
        var client = new ClientIdentity("client");

        Assert.That(
            () => new ClientSnapshot(
                new SnapshotSequence(1),
                new MacroTimestamp(TimeSpan.FromSeconds(2)),
                new MacroTimestamp(TimeSpan.FromSeconds(1)),
                client,
                SnapshotQuality.Complete,
                ClientPresence.InWorld),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void ShouldRejectUnsupportedObservedPanel()
    {
        var client = new ClientIdentity("client");

        Assert.That(
            () => new ClientSnapshot(
                new SnapshotSequence(1),
                MacroTimestamp.Zero,
                MacroTimestamp.Zero,
                client,
                SnapshotQuality.Complete,
                ClientPresence.InWorld,
                (ClientPanel)int.MaxValue),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ShouldPreserveOptionalSnapshotSections()
    {
        var character = new CharacterSnapshot(
            CharacterClass.Wizard,
            level: 99,
            abilityLevel: 50);
        var inventory = new InventorySnapshot(
        [
            new InventoryItemSnapshot(1, "staff")
        ]);
        var equipment = new EquipmentSnapshot("weapon");
        var vitals = new VitalsSnapshot(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana: 50,
            maximumMana: 100);
        var spellbook = new SpellbookSnapshot(
        [
            new SpellSnapshot(
                "spell",
                slot: 1,
                currentLevel: 1,
                maximumLevel: 100,
                castLines: 1,
                manaCost: 10,
                cooldown: TimeSpan.Zero)
        ]);
        var skillbook = new SkillbookSnapshot(
        [
            new SkillSnapshot(
                "skill",
                slot: 1,
                currentLevel: 1,
                maximumLevel: 100,
                manaCost: 0,
                cooldown: TimeSpan.Zero)
        ]);
        var location = new MapLocationSnapshot(
            mapNumber: 1,
            mapName: "map",
            x: 50,
            y: 50);
        var snapshot = new ClientSnapshot(
            new SnapshotSequence(1),
            MacroTimestamp.Zero,
            MacroTimestamp.Zero,
            new ClientIdentity("client"),
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            ClientPanel.Inventory,
            character,
            inventory,
            equipment,
            vitals,
            spellbook,
            skillbook,
            location,
            isInventoryExpanded: true);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Character, Is.EqualTo(character));
            Assert.That(snapshot.Inventory, Is.EqualTo(inventory));
            Assert.That(snapshot.Equipment, Is.EqualTo(equipment));
            Assert.That(snapshot.Vitals, Is.EqualTo(vitals));
            Assert.That(snapshot.Spellbook, Is.EqualTo(spellbook));
            Assert.That(snapshot.Skillbook, Is.EqualTo(skillbook));
            Assert.That(snapshot.Location, Is.EqualTo(location));
            Assert.That(snapshot.IsInventoryExpanded, Is.True);
        });
    }
}
