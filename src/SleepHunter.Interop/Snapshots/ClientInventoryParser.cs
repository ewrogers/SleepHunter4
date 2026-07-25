using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientInventoryParser
{
    public const int RecordSize = 0x106;
    public const int RecordCount = 60;
    public const int NameLength = 256;

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

            items.Add(new InventoryItemSnapshot(slot, name));
        }

        return new InventorySnapshot(items);
    }
}
