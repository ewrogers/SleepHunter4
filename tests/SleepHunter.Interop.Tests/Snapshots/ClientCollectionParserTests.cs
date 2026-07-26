using System.Buffers.Binary;
using System.Text;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Tests.Snapshots;

public sealed class ClientCollectionParserTests
{
    [Test]
    public void ShouldParseCompactInventoryAndExcludeGoldSlot()
    {
        var snapshot = new byte[
            ClientInventoryParser.RecordSize *
            ClientInventoryParser.RecordCount];
        WriteInventoryItem(snapshot, slot: 1, "Ring2");
        WriteInventoryItem(snapshot, slot: 3, "Gnarl");
        WriteInventoryItem(snapshot, slot: 60, "Gold");

        var inventory = ClientInventoryParser.Parse(
            snapshot,
            ClientInventoryParser.RecordCount);

        Assert.That(
            inventory,
            Is.EqualTo(
                new InventorySnapshot(
                [
                    new InventoryItemSnapshot(1, "Ring2"),
                    new InventoryItemSnapshot(3, "Gnarl")
                ])));
    }

    [Test]
    public void ShouldRejectInvalidInventoryLengthNameAndEncoding()
    {
        var blankName = new byte[ClientInventoryParser.RecordSize];
        blankName[0] = 1;
        var invalidEncoding = new byte[ClientInventoryParser.RecordSize];
        invalidEncoding[0] = 1;
        invalidEncoding[5] = 0xFF;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidDataException>(
                () => ClientInventoryParser.Parse(
                    new byte[1],
                    recordCount: 1));
            Assert.Throws<InvalidDataException>(
                () => ClientInventoryParser.Parse(
                    blankName,
                    recordCount: 1));
            Assert.Throws<InvalidDataException>(
                () => ClientInventoryParser.Parse(
                    invalidEncoding,
                    recordCount: 1));
        });
    }

    [Test]
    public void ShouldParseRichWeaponAndShieldSlots()
    {
        var snapshot = new byte[
            ClientEquipmentParser.RichSnapshotSize];
        WriteRichEquipmentItem(
            snapshot,
            slotIndex: 0,
            rawSprite: 0x8123,
            dyeColor: 1,
            "Holy Diana",
            currentDurability: 29976,
            maximumDurability: 30000);
        WriteRichEquipmentItem(
            snapshot,
            slotIndex: 2,
            rawSprite: 0x8456,
            dyeColor: 2,
            "Dragon Shield");

        var equipment = ClientEquipmentParser.ParseRich(
            snapshot,
            ClientEquipmentParser.RecordCount);

        Assert.That(
            equipment,
            Is.EqualTo(
                new EquipmentSnapshot(
                [
                    new EquipmentItemSnapshot(
                        1,
                        "Holy Diana",
                        sprite: 0x8123,
                        dyeColor: 1,
                        currentDurability: 29976,
                        maximumDurability: 30000),
                    new EquipmentItemSnapshot(
                        3,
                        "Dragon Shield",
                        sprite: 0x8456,
                        dyeColor: 2)
                ])));
    }

    [Test]
    public void ShouldRequireRichSpritePresenceAndExactLength()
    {
        var snapshot = new byte[
            ClientEquipmentParser.RichSnapshotSize];
        Encoding.ASCII.GetBytes("Stale Name").CopyTo(snapshot.AsSpan(0x36));

        var equipment = ClientEquipmentParser.ParseRich(
            snapshot,
            ClientEquipmentParser.RecordCount);

        Assert.Multiple(() =>
        {
            Assert.That(equipment.IsDisarmed, Is.True);
            Assert.Throws<InvalidDataException>(
                () => ClientEquipmentParser.ParseRich(
                    new byte[1],
                    ClientEquipmentParser.RecordCount));
        });
    }

    [Test]
    public void ShouldParseCompactEquipmentFallback()
    {
        var snapshot = new byte[
            ClientEquipmentParser.CompactNameLength *
            ClientEquipmentParser.RecordCount];
        WriteCompactEquipmentItem(snapshot, slotIndex: 0, "Holy Diana");
        WriteCompactEquipmentItem(
            snapshot,
            slotIndex: 2,
            "Dragon Shield");

        var equipment = ClientEquipmentParser.ParseCompact(
            snapshot,
            ClientEquipmentParser.RecordCount,
            ClientEquipmentParser.CompactNameLength);

        Assert.That(
            equipment,
            Is.EqualTo(
                new EquipmentSnapshot(
                    "Holy Diana",
                    "Dragon Shield")));
    }

    [Test]
    public void ShouldParseRichInventoryPaneFields()
    {
        var snapshot = new byte[ClientInventoryParser.PaneSnapshotSize];
        BinaryPrimitives.WriteUInt16LittleEndian(snapshot, 0x8123);
        Encoding.ASCII.GetBytes("Holy Diana [29976/30000]").CopyTo(
            snapshot.AsSpan(0x02));
        snapshot[0x82] = 3;
        snapshot[0x84] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0xA8),
            30000);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0xAC),
            29976);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0xB0),
            1);

        var item = ClientInventoryParser.ParsePane(
            snapshot,
            expectedSlot: 1,
            compactName: "Holy Diana",
            compactSprite: 0x8123);

        Assert.That(
            item,
            Is.EqualTo(
                new InventoryItemSnapshot(
                    1,
                    "Holy Diana",
                    sprite: 0x8123,
                    dyeColor: 3,
                    displayName: "Holy Diana [29976/30000]",
                    currentDurability: 29976,
                    maximumDurability: 30000)));
    }

    [Test]
    public void ShouldParseGroupRosterAndSpellEffectStages()
    {
        var groupBytes = new byte[ClientGroupParser.RecordSize * 2];
        Encoding.ASCII.GetBytes("Aislinn").CopyTo(groupBytes);
        groupBytes[ClientGroupParser.NameLength] = 1;
        Encoding.ASCII.GetBytes("Eidolon").CopyTo(
            groupBytes.AsSpan(ClientGroupParser.RecordSize));

        var effectBytes = new byte[ClientSpellEffectParser.SnapshotSize];
        for (var index = 0;
             index < ClientSpellEffectParser.RecordCount;
             index++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(
                effectBytes.AsSpan(index * sizeof(short)),
                -1);
        }

        BinaryPrimitives.WriteInt16LittleEndian(
            effectBytes,
            321);
        effectBytes[
            ClientSpellEffectParser.RecordCount * sizeof(short)] =
            (byte)SpellEffectDurationStage.White;

        Assert.Multiple(() =>
        {
            Assert.That(
                ClientGroupParser.Parse(groupBytes, recordCount: 2),
                Is.EqualTo(
                    new GroupSnapshot(
                    [
                        new GroupMemberSnapshot(
                            "Aislinn",
                            isStarred: true),
                        new GroupMemberSnapshot(
                            "Eidolon",
                            isStarred: false)
                    ])));
            Assert.That(
                ClientSpellEffectParser.Parse(effectBytes),
                Is.EqualTo(
                    new ActiveSpellEffectsSnapshot(
                    [
                        new ActiveSpellEffectSnapshot(
                            1,
                            icon: 321,
                            SpellEffectDurationStage.White)
                    ])));
        });
    }

    [Test]
    public void ShouldRejectUnsupportedSpellEffectStage()
    {
        var effectBytes = new byte[ClientSpellEffectParser.SnapshotSize];
        for (var index = 0;
             index < ClientSpellEffectParser.RecordCount;
             index++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(
                effectBytes.AsSpan(index * sizeof(short)),
                -1);
        }

        BinaryPrimitives.WriteInt16LittleEndian(effectBytes, 321);
        effectBytes[
            ClientSpellEffectParser.RecordCount * sizeof(short)] = 7;

        Assert.Throws<InvalidDataException>(
            () => ClientSpellEffectParser.Parse(effectBytes));
    }

    private static void WriteInventoryItem(
        Span<byte> snapshot,
        int slot,
        string name)
    {
        var record = snapshot.Slice(
            (slot - 1) * ClientInventoryParser.RecordSize,
            ClientInventoryParser.RecordSize);
        record[0] = 1;
        Encoding.ASCII.GetBytes(name).CopyTo(record[5..]);
    }

    private static void WriteRichEquipmentItem(
        Span<byte> snapshot,
        int slotIndex,
        ushort rawSprite,
        byte dyeColor,
        string name,
        uint currentDurability = 0,
        uint maximumDurability = 0)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(
            snapshot.Slice(slotIndex * sizeof(ushort)),
            rawSprite);
        snapshot[0x24 + slotIndex] = dyeColor;
        Encoding.ASCII.GetBytes(name).CopyTo(
            snapshot.Slice(
                0x36 +
                slotIndex * ClientEquipmentParser.CompactNameLength));
        var durabilityOffset = 0x938 + slotIndex * 0x08;
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.Slice(durabilityOffset),
            maximumDurability);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.Slice(durabilityOffset + sizeof(uint)),
            currentDurability);
    }

    private static void WriteCompactEquipmentItem(
        Span<byte> snapshot,
        int slotIndex,
        string name) =>
        Encoding.ASCII.GetBytes(name).CopyTo(
            snapshot.Slice(
                slotIndex *
                ClientEquipmentParser.CompactNameLength));
}
