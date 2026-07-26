using System.Buffers.Binary;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientInventoryParser
{
    public const int RecordSize = 0x106;
    public const int RecordCount = 60;
    public const int NameLength = 256;
    public const int PanePointerSize = sizeof(uint);
    public const int PaneSnapshotOffset = 0x190;
    public const int PaneSnapshotSize = 0xB8;

    private const int NameOffset = 5;
    private const int GoldSlot = 60;

    public static InventorySnapshot Parse(
        ReadOnlySpan<byte> snapshot,
        int recordCount)
    {
        if (recordCount < 0 || recordCount > RecordCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordCount),
                recordCount,
                $"Inventory record count must be between 0 and {RecordCount}.");
        }

        var expectedLength = checked(recordCount * RecordSize);
        if (snapshot.Length != expectedLength)
        {
            throw new InvalidDataException(
                $"An inventory snapshot with {recordCount} records must contain {expectedLength} bytes.");
        }

        var items = new List<InventoryItemSnapshot>(recordCount);
        for (var index = 0; index < recordCount; index++)
        {
            var slot = index + 1;
            if (slot == GoldSlot)
            {
                continue;
            }

            var record = snapshot.Slice(index * RecordSize, RecordSize);
            if (record[0] == 0)
            {
                continue;
            }

            var name = ClientText.ReadNullTerminatedAscii(
                record.Slice(NameOffset, NameLength));
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidDataException(
                    $"Inventory slot {slot} is marked present but has no name.");
            }

            items.Add(new InventoryItemSnapshot(
                slot,
                name,
                BinaryPrimitives.ReadUInt16LittleEndian(
                    record.Slice(2, sizeof(ushort))),
                record[4]));
        }

        return new InventorySnapshot(items);
    }

    public static InventoryItemSnapshot ParsePane(
        ReadOnlySpan<byte> snapshot,
        int expectedSlot,
        string compactName,
        ushort compactSprite)
    {
        if (snapshot.Length != PaneSnapshotSize)
        {
            throw new InvalidDataException(
                $"An inventory pane snapshot must contain {PaneSnapshotSize} bytes.");
        }

        var sprite = BinaryPrimitives.ReadUInt16LittleEndian(snapshot);
        var displayName = ClientText.ReadNullTerminatedAscii(
            snapshot.Slice(0x02, 0x80));
        var slot = snapshot[0x84];
        var currentDurability = BinaryPrimitives.ReadUInt32LittleEndian(
            snapshot.Slice(0xA8, sizeof(uint)));
        var maximumDurability = BinaryPrimitives.ReadUInt32LittleEndian(
            snapshot.Slice(0xAC, sizeof(uint)));
        var quantity = BinaryPrimitives.ReadUInt32LittleEndian(
            snapshot.Slice(0xB0, sizeof(uint)));
        var isStackable = snapshot[0xB4] != 0;

        if (slot != expectedSlot ||
            sprite != compactSprite ||
            string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidDataException(
                $"Inventory pane slot {expectedSlot} does not match the compact inventory record.");
        }

        return new InventoryItemSnapshot(
            slot,
            compactName,
            sprite,
            snapshot[0x82],
            displayName,
            quantity == 0 ? 1 : quantity,
            isStackable,
            currentDurability,
            maximumDurability);
    }
}
