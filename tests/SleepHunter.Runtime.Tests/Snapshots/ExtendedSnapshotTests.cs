using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class ExtendedSnapshotTests
{
    [Test]
    public void ShouldPreserveRichCharacterAndMapState()
    {
        var character = new CharacterSnapshot(
            CharacterClass.Wizard,
            level: 99,
            abilityLevel: 50,
            name: "Aislinn",
            characterId: 1234,
            CharacterUserState.Grouped,
            gold: 123456,
            strength: 10,
            armorClass: -10,
            actionState: 1);
        var location = new MapLocationSnapshot(
            1,
            "Mileth",
            x: 50,
            y: 60,
            width: 100,
            height: 100,
            flags: 0x12,
            weather: 2);

        Assert.Multiple(() =>
        {
            Assert.That(
                character.UserState,
                Is.EqualTo(CharacterUserState.Grouped));
            Assert.That(character.Gold, Is.EqualTo(123456));
            Assert.That(character.Strength, Is.EqualTo(10));
            Assert.That(character.ArmorClass, Is.EqualTo(-10));
            Assert.That(character.IsActionLocked, Is.True);
            Assert.That(location.Width, Is.EqualTo(100));
            Assert.That(location.Height, Is.EqualTo(100));
            Assert.That(location.Flags, Is.EqualTo(0x12));
            Assert.That(location.Weather, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldPreserveAllEquipmentSlotsAndItemPaneState()
    {
        var inventoryItem = new InventoryItemSnapshot(
            7,
            "Viper's Gland",
            sprite: 0x8123,
            dyeColor: 6,
            displayName: "Viper's Gland [ 12 ]",
            quantity: 12,
            isStackable: true,
            currentDurability: 12345,
            maximumDurability: 15000);
        var equipment = new EquipmentSnapshot(
        [
            new EquipmentItemSnapshot(
                1,
                "Holy Diana",
                sprite: 0x8123),
            new EquipmentItemSnapshot(
                18,
                "Winter Scarf",
                sprite: 0x8D24,
                dyeColor: 1,
                currentDurability: 29976,
                maximumDurability: 30000)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(inventoryItem.Quantity, Is.EqualTo(12));
            Assert.That(inventoryItem.IsStackable, Is.True);
            Assert.That(
                inventoryItem.CurrentDurability,
                Is.EqualTo(12345));
            Assert.That(
                inventoryItem.MaximumDurability,
                Is.EqualTo(15000));
            Assert.That(equipment.Items.Length, Is.EqualTo(2));
            Assert.That(equipment.Find(18)?.Name, Is.EqualTo("Winter Scarf"));
            Assert.That(
                equipment.Find(18)?.CurrentDurability,
                Is.EqualTo(29976));
        });
    }

    [Test]
    public void ShouldRejectDuplicateGroupAndWorldEntries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new GroupSnapshot(
                [
                    new GroupMemberSnapshot("Aislinn", false),
                    new GroupMemberSnapshot("aislinn", true)
                ]),
                Throws.ArgumentException);
            Assert.That(
                () => new WorldEntitiesSnapshot(
                [
                    new WorldEntitySnapshot(
                        1,
                        WorldEntityType.Monster,
                        x: 1,
                        y: 1),
                    new WorldEntitySnapshot(
                        1,
                        WorldEntityType.GroundItem,
                        x: 2,
                        y: 2)
                ]),
                Throws.ArgumentException);
        });
    }
}
