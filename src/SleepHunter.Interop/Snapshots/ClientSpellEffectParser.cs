using System.Buffers.Binary;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientSpellEffectParser
{
    public const int RecordCount = 10;
    public const int SnapshotSize =
        RecordCount * sizeof(short) +
        RecordCount * sizeof(sbyte);

    public static ActiveSpellEffectsSnapshot Parse(
        ReadOnlySpan<byte> snapshot)
    {
        if (snapshot.Length != SnapshotSize)
        {
            throw new InvalidDataException(
                $"An active spell effect snapshot must contain {SnapshotSize} bytes.");
        }

        var effects = new List<ActiveSpellEffectSnapshot>(RecordCount);
        for (var index = 0; index < RecordCount; index++)
        {
            var icon = BinaryPrimitives.ReadInt16LittleEndian(
                snapshot.Slice(
                    index * sizeof(short),
                    sizeof(short)));
            var rawStage = unchecked(
                (sbyte)snapshot[
                    RecordCount * sizeof(short) +
                    index]);

            if (icon == -1 || rawStage == 0)
            {
                continue;
            }

            if (icon < 0 ||
                rawStage is <
                    (sbyte)SpellEffectDurationStage.Blue or >
                    (sbyte)SpellEffectDurationStage.White)
            {
                throw new InvalidDataException(
                    $"Active spell effect slot {index + 1} contains an unsupported icon or duration stage.");
            }

            effects.Add(
                new ActiveSpellEffectSnapshot(
                    index + 1,
                    (ushort)icon,
                    (SpellEffectDurationStage)rawStage));
        }

        return new ActiveSpellEffectsSnapshot(effects);
    }
}
