using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientGroupParser
{
    public const int RecordSize = 0x41;
    public const int RecordCount = 64;
    public const int NameLength = 0x40;

    public static GroupSnapshot Parse(
        ReadOnlySpan<byte> snapshot,
        int recordCount)
    {
        if (recordCount is < 0 or > RecordCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordCount),
                recordCount,
                $"Group member count must be between 0 and {RecordCount}.");
        }

        var expectedLength = checked(recordCount * RecordSize);
        if (snapshot.Length != expectedLength)
        {
            throw new InvalidDataException(
                $"A group snapshot with {recordCount} records must contain {expectedLength} bytes.");
        }

        var members = new List<GroupMemberSnapshot>(recordCount);
        for (var index = 0; index < recordCount; index++)
        {
            var record = snapshot.Slice(
                index * RecordSize,
                RecordSize);
            var name = ClientText.ReadNullTerminatedAscii(
                record[..NameLength]);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidDataException(
                    $"Group member record {index} has no name.");
            }

            members.Add(
                new GroupMemberSnapshot(
                    name,
                    record[NameLength] != 0));
        }

        try
        {
            return new GroupSnapshot(members);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "The client group roster contains conflicting members.",
                exception);
        }
    }
}
