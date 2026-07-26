using SleepHunter.Models;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Tests.Models;

public sealed class PlayerSnapshotProjectionTests
{
    [Test]
    public void ShouldProjectACompleteRuntimeSnapshotIntoUiModels()
    {
        using var player = CreatePlayer();

        player.ApplySnapshot(CreateInWorldSnapshot(sequence: 1));

        var inventoryItem = player.Inventory.ItemsAndGold
            .Single(item => item.Slot == 7);
        var gold = player.Inventory.ItemsAndGold
            .Single(item => item.IsGold);
        var equipment = player.Equipment.Single();
        var skill = player.Skillbook.GetSkill("Assail");
        var spell = player.Spellbook.GetSpell(
            Spell.LyliacPlantKey);

        Assert.Multiple(() =>
        {
            Assert.That(player.IsLoggedIn, Is.True);
            Assert.That(player.Name, Is.EqualTo("Runtime"));
            Assert.That(player.LastSnapshotSequence, Is.EqualTo(1));
            Assert.That(player.Stats.CurrentHealth, Is.EqualTo(300));
            Assert.That(player.Stats.MaximumMana, Is.EqualTo(600));
            Assert.That(player.Location.MapName, Is.EqualTo("Mileth"));
            Assert.That(player.Location.X, Is.EqualTo(12));
            Assert.That(inventoryItem.Name, Is.EqualTo("Viper's Gland"));
            Assert.That(inventoryItem.Quantity, Is.EqualTo(12));
            Assert.That(inventoryItem.Durability, Is.EqualTo(900));
            Assert.That(gold.Quantity, Is.EqualTo(3700));
            Assert.That(equipment.Name, Is.EqualTo("Bardocle"));
            Assert.That(skill, Is.Not.Null);
            Assert.That(skill.MinHealthPercent, Is.EqualTo(25));
            Assert.That(skill.CooldownRemainingFraction, Is.EqualTo(0.5));
            Assert.That(spell, Is.Not.Null);
            Assert.That(spell!.NumberOfLines, Is.EqualTo(3));
            Assert.That(player.HasLyliacPlant, Is.True);
            Assert.That(player.HasLyliacVineyard, Is.False);
        });
    }

    [Test]
    public void ShouldIgnoreStaleSnapshotsAndClearOnRuntimeLogout()
    {
        using var player = CreatePlayer();
        player.ApplySnapshot(CreateInWorldSnapshot(sequence: 2));

        player.ApplySnapshot(CreateLoggedOutSnapshot(sequence: 1));

        Assert.Multiple(() =>
        {
            Assert.That(player.IsLoggedIn, Is.True);
            Assert.That(player.Stats.CurrentHealth, Is.EqualTo(300));
        });

        player.ApplySnapshot(CreateLoggedOutSnapshot(sequence: 3));

        Assert.Multiple(() =>
        {
            Assert.That(player.IsLoggedIn, Is.False);
            Assert.That(player.Name, Is.EqualTo("Runtime"));
            Assert.That(player.LastSnapshotSequence, Is.EqualTo(3));
            Assert.That(player.Stats.CurrentHealth, Is.Zero);
            Assert.That(player.Location.MapName, Is.Null);
            Assert.That(player.Equipment, Is.Empty);
            Assert.That(player.Skillbook, Is.Empty);
            Assert.That(player.Spellbook, Is.Empty);
            Assert.That(player.HasLyliacPlant, Is.False);
        });
    }

    private static Player CreatePlayer() =>
        new(
            new ClientProcess
            {
                ProcessId = Environment.ProcessId,
                WindowHandle = new nint(1)
            });

    private static ClientSnapshot CreateInWorldSnapshot(
        long sequence)
    {
        var timestamp = new MacroTimestamp(
            TimeSpan.FromTicks(sequence));
        return new ClientSnapshot(
            new SnapshotSequence(sequence),
            timestamp,
            timestamp,
            new ClientIdentity("test"),
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            character: new CharacterSnapshot(
                CharacterClass.Wizard,
                level: 99,
                abilityLevel: 50,
                name: "Runtime",
                gold: 3700),
            inventory: new InventorySnapshot(
                [
                    new InventoryItemSnapshot(
                        slot: 7,
                        name: "Viper's Gland",
                        sprite: 0x8123,
                        dyeColor: 6,
                        displayName: "Viper's Gland [ 12 ]",
                        quantity: 12,
                        isStackable: true,
                        currentDurability: 900,
                        maximumDurability: 1000)
                ]),
            equipment: new EquipmentSnapshot(
                [
                    new EquipmentItemSnapshot(
                        slot: 1,
                        name: "Bardocle",
                        sprite: 0x809A,
                        currentDurability: 800,
                        maximumDurability: 1000)
                ]),
            vitals: new VitalsSnapshot(
                currentHealth: 300,
                maximumHealth: 400,
                currentMana: 500,
                maximumMana: 600),
            spellbook: new SpellbookSnapshot(
                [
                    new SpellSnapshot(
                        Spell.LyliacPlantKey,
                        slot: 1,
                        currentLevel: 1,
                        maximumLevel: 100,
                        castLines: 3,
                        manaCost: 200,
                        cooldown: TimeSpan.FromSeconds(2),
                        icon: 14)
                ]),
            skillbook: new SkillbookSnapshot(
                [
                    new SkillSnapshot(
                        "Assail",
                        slot: 1,
                        currentLevel: 10,
                        maximumLevel: 100,
                        manaCost: 0,
                        cooldown: TimeSpan.FromSeconds(1),
                        isAssail: true,
                        healthCondition: new HealthCondition(
                            minimumPercentExclusive: 25),
                        icon: 12,
                        cooldownProgress: 15,
                        isCooldownVisualActive: true)
                ]),
            location: new MapLocationSnapshot(
                mapNumber: 100,
                mapName: "Mileth",
                x: 12,
                y: 34));
    }

    private static ClientSnapshot CreateLoggedOutSnapshot(
        long sequence)
    {
        var timestamp = new MacroTimestamp(
            TimeSpan.FromTicks(sequence));
        return new ClientSnapshot(
            new SnapshotSequence(sequence),
            timestamp,
            timestamp,
            new ClientIdentity("test"),
            SnapshotQuality.Complete,
            ClientPresence.LoggedOut);
    }
}
