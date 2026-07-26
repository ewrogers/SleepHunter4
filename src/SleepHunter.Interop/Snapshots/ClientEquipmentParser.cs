using System.Buffers.Binary;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientEquipmentParser
{
    public const int RecordCount = 18;
    public const int RichSnapshotSize = 0x9C8;
    public const int CompactNameLength = 128;

    private const int DyeArrayOffset = 0x24;
    private const int NameArrayOffset = 0x36;
    private const int DurabilityArrayOffset = 0x938;
    private const int DurabilityRecordSize = 0x08;

    public static EquipmentSnapshot ParseRich(
        ReadOnlySpan<byte> snapshot,
        int recordCount)
    {
        ValidateRecordCount(recordCount);
        if (snapshot.Length != RichSnapshotSize)
        {
            throw new InvalidDataException(
                $"An equipment snapshot must contain {RichSnapshotSize} bytes.");
        }

        var items = new List<EquipmentItemSnapshot>(recordCount);
        for (var index = 0; index < recordCount; index++)
        {
            var rawSprite = BinaryPrimitives.ReadUInt16LittleEndian(
                snapshot.Slice(
                    index * sizeof(ushort),
                    sizeof(ushort)));
            var name = ReadRichName(snapshot, index);
            if (rawSprite == 0 || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var durabilityOffset =
                DurabilityArrayOffset +
                index * DurabilityRecordSize;
            var currentDurability =
                BinaryPrimitives.ReadUInt32LittleEndian(
                    snapshot.Slice(
                        durabilityOffset,
                        sizeof(uint)));
            var maximumDurability =
                BinaryPrimitives.ReadUInt32LittleEndian(
                    snapshot.Slice(
                        durabilityOffset + sizeof(uint),
                        sizeof(uint)));
            items.Add(
                new EquipmentItemSnapshot(
                    index + 1,
                    name,
                    rawSprite,
                    snapshot[DyeArrayOffset + index],
                    currentDurability,
                    maximumDurability));
        }

        return new EquipmentSnapshot(items);
    }

    public static EquipmentSnapshot ParseCompact(
        ReadOnlySpan<byte> snapshot,
        int recordCount,
        int nameLength)
    {
        ValidateRecordCount(recordCount);
        if (nameLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nameLength),
                nameLength,
                "The compact equipment name length must be positive.");
        }

        var expectedLength = checked(recordCount * nameLength);
        if (snapshot.Length != expectedLength)
        {
            throw new InvalidDataException(
                $"A compact equipment snapshot with {recordCount} records must contain {expectedLength} bytes.");
        }

        var items = new List<EquipmentItemSnapshot>(recordCount);
        for (var index = 0; index < recordCount; index++)
        {
            var name = ReadCompactName(snapshot, index, nameLength);
            if (!string.IsNullOrWhiteSpace(name))
            {
                items.Add(
                    new EquipmentItemSnapshot(
                        index + 1,
                        name));
            }
        }

        return new EquipmentSnapshot(items);
    }

    private static string? ReadRichName(
        ReadOnlySpan<byte> snapshot,
        int index)
    {
        var rawSprite = BinaryPrimitives.ReadUInt16LittleEndian(
            snapshot.Slice(index * sizeof(ushort), sizeof(ushort)));
        if (rawSprite == 0)
        {
            return null;
        }

        var name = ClientText.ReadNullTerminatedAscii(
            snapshot.Slice(
                NameArrayOffset + index * CompactNameLength,
                CompactNameLength));
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name;
    }

    private static string? ReadCompactName(
        ReadOnlySpan<byte> snapshot,
        int index,
        int nameLength)
    {
        var name = ClientText.ReadNullTerminatedAscii(
            snapshot.Slice(index * nameLength, nameLength));
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name;
    }

    private static void ValidateRecordCount(int recordCount)
    {
        if (recordCount < 0 || recordCount > RecordCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordCount),
                recordCount,
                $"Equipment record count must be between 0 and {RecordCount}.");
        }
    }
}
