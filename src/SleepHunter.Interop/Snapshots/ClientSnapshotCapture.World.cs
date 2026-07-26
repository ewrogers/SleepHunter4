using SleepHunter.Interop.Mappings;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

public sealed partial class ClientSnapshotCapture
{
    private static bool TryReadGroup(
        MappedMemoryReader reader,
        out GroupSnapshot? group,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryReadUInt32(
                GroupMemberCountKey,
                out var rawCount,
                out var countError))
        {
            group = null;
            error = MappingFailure(
                SnapshotSection.Group,
                GroupMemberCountKey,
                countError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (rawCount > ClientGroupParser.RecordCount)
        {
            group = null;
            error = InvalidValue(
                SnapshotSection.Group,
                GroupMemberCountKey,
                $"Group member count {rawCount} exceeds the supported roster capacity.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        var count = (int)rawCount;
        if (count == 0)
        {
            group = GroupSnapshot.Empty;
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        if (!TryReadStableBlock(
                reader,
                GroupMemberCacheKey,
                SnapshotSection.Group,
                checked(count * ClientGroupParser.RecordSize),
                out var bytes,
                out error,
                out failureQuality))
        {
            group = null;
            return false;
        }

        if (!reader.TryReadUInt32(
                GroupMemberCountKey,
                out var currentCount,
                out countError))
        {
            group = null;
            error = MappingFailure(
                SnapshotSection.Group,
                GroupMemberCountKey,
                countError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (currentCount != rawCount)
        {
            group = null;
            error = StateChanged(
                SnapshotSection.Group,
                GroupMemberCountKey,
                "The group roster changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        try
        {
            group = ClientGroupParser.Parse(bytes, count);
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }
        catch (InvalidDataException exception)
        {
            group = null;
            error = InvalidValue(
                SnapshotSection.Group,
                GroupMemberCacheKey,
                exception.Message);
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }
    }

    private static bool TryReadActiveSpellEffects(
        MappedMemoryReader reader,
        out ActiveSpellEffectsSnapshot? activeSpellEffects,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!TryReadStableBlock(
                reader,
                ActiveSpellEffectsKey,
                SnapshotSection.ActiveSpellEffects,
                ClientSpellEffectParser.SnapshotSize,
                out var bytes,
                out error,
                out failureQuality))
        {
            activeSpellEffects = null;
            return false;
        }

        try
        {
            activeSpellEffects = ClientSpellEffectParser.Parse(bytes);
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }
        catch (InvalidDataException exception)
        {
            activeSpellEffects = null;
            error = InvalidValue(
                SnapshotSection.ActiveSpellEffects,
                ActiveSpellEffectsKey,
                exception.Message);
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }
    }

    private static bool TryReadWorldEntities(
        MappedMemoryReader reader,
        uint localCharacterId,
        out WorldEntitiesSnapshot? worldEntities,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryResolveAddress(
                WorldObjectListKey,
                out var worldObjectList,
                out var listError))
        {
            worldEntities = null;
            error = MappingFailure(
                SnapshotSection.WorldEntities,
                WorldObjectListKey,
                listError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!ClientWorldEntityReader.TryRead(
                reader.Session,
                worldObjectList,
                localCharacterId,
                out var entities,
                out var entityError))
        {
            worldEntities = null;
            error = InvalidValue(
                SnapshotSection.WorldEntities,
                WorldObjectListKey,
                entityError ??
                "The world entity collection could not be captured.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryResolveAddress(
                WorldObjectListKey,
                out var currentWorldObjectList,
                out listError))
        {
            worldEntities = null;
            error = MappingFailure(
                SnapshotSection.WorldEntities,
                WorldObjectListKey,
                listError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (currentWorldObjectList != worldObjectList)
        {
            worldEntities = null;
            error = StateChanged(
                SnapshotSection.WorldEntities,
                WorldObjectListKey,
                "The world entity collection changed owners during capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        worldEntities = entities;
        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }
}
