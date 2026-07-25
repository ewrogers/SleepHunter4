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
        var client = new ClientIdentity("client", "test");

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
        var client = new ClientIdentity("client", "test");

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
        var client = new ClientIdentity("client", "test");

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
    public void ShouldPreserveCharacterInventoryAndEquipmentSections()
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
        var snapshot = new ClientSnapshot(
            new SnapshotSequence(1),
            MacroTimestamp.Zero,
            MacroTimestamp.Zero,
            new ClientIdentity("client", "test"),
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            ClientPanel.Inventory,
            character,
            inventory,
            equipment);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Character, Is.EqualTo(character));
            Assert.That(snapshot.Inventory, Is.EqualTo(inventory));
            Assert.That(snapshot.Equipment, Is.EqualTo(equipment));
        });
    }
}
