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
            "Holy Diana");
        WriteRichEquipmentItem(
            snapshot,
            slotIndex: 2,
            rawSprite: 0x8456,
            "Dragon Shield");

        var equipment = ClientEquipmentParser.ParseRich(
            snapshot,
            ClientEquipmentParser.RecordCount);

        Assert.That(
            equipment,
            Is.EqualTo(
                new EquipmentSnapshot(
                    "Holy Diana",
                    "Dragon Shield")));
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
        string name)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(
            snapshot.Slice(slotIndex * sizeof(ushort)),
            rawSprite);
        Encoding.ASCII.GetBytes(name).CopyTo(
            snapshot.Slice(
                0x36 +
                slotIndex * ClientEquipmentParser.CompactNameLength));
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
