using System.Buffers.Binary;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class Usda741EquipmentParser
{
    public const int RecordCount = 18;
    public const int RichSnapshotSize = 0x9C8;
    public const int CompactNameLength = 128;

    private const int WeaponIndex = 0;
    private const int ShieldIndex = 2;
    private const int NameArrayOffset = 0x36;

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

        var weaponName = recordCount > WeaponIndex
            ? ReadRichName(snapshot, WeaponIndex)
            : null;
        var shieldName = recordCount > ShieldIndex
            ? ReadRichName(snapshot, ShieldIndex)
            : null;
        return new EquipmentSnapshot(weaponName, shieldName);
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

        var weaponName = recordCount > WeaponIndex
            ? ReadCompactName(snapshot, WeaponIndex, nameLength)
            : null;
        var shieldName = recordCount > ShieldIndex
            ? ReadCompactName(snapshot, ShieldIndex, nameLength)
            : null;
        return new EquipmentSnapshot(weaponName, shieldName);
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

        var name = Usda741Text.ReadNullTerminatedAscii(
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
        var name = Usda741Text.ReadNullTerminatedAscii(
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
