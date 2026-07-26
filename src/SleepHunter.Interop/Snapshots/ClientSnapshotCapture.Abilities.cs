using System.Buffers.Binary;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

public sealed partial class ClientSnapshotCapture
{
    private bool TryReadSkillbook(
        MappedMemoryReader reader,
        out SkillbookSnapshot? skillbook,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (TryReadSkillbookPanes(reader, out skillbook))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        var definition = reader.Map.Find(SkillbookKey)!;
        var length = checked(definition.RecordSize * definition.Capacity);
        if (!TryReadStableBlock(
                reader,
                SkillbookKey,
                SnapshotSection.Skillbook,
                length,
                out var bytes,
                out error,
                out failureQuality))
        {
            skillbook = null;
            return false;
        }

        try
        {
            skillbook = ClientAbilityParser.ParseCompactSkills(
                bytes,
                definition.Capacity,
                abilityCatalog);
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }
        catch (InvalidDataException exception)
        {
            skillbook = null;
            error = InvalidValue(
                SnapshotSection.Skillbook,
                SkillbookKey,
                exception.Message);
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }
    }

    private bool TryReadSpellbook(
        MappedMemoryReader reader,
        out SpellbookSnapshot? spellbook,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (TryReadSpellbookPanes(reader, out spellbook))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        var definition = reader.Map.Find(SpellbookKey)!;
        var length = checked(definition.RecordSize * definition.Capacity);
        if (!TryReadStableBlock(
                reader,
                SpellbookKey,
                SnapshotSection.Spellbook,
                length,
                out var bytes,
                out error,
                out failureQuality))
        {
            spellbook = null;
            return false;
        }

        try
        {
            spellbook = ClientAbilityParser.ParseCompactSpells(
                bytes,
                definition.Capacity,
                abilityCatalog);
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }
        catch (InvalidDataException exception)
        {
            spellbook = null;
            error = InvalidValue(
                SnapshotSection.Spellbook,
                SpellbookKey,
                exception.Message);
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }
    }

    private bool TryReadSkillbookPanes(
        MappedMemoryReader reader,
        out SkillbookSnapshot? skillbook)
    {
        if (!TryReadPaneTable(
                reader,
                SkillbookPanesKey,
                SkillbookPaneCapacityKey,
                out var observation))
        {
            skillbook = null;
            return false;
        }

        try
        {
            var skills = new List<SkillSnapshot>(observation.PointerCount);
            var slots = new HashSet<int>();
            foreach (var paneAddress in observation.PaneAddresses)
            {
                if (paneAddress.IsNull)
                {
                    continue;
                }

                if (!TryReadPaneSnapshot(
                        reader.Session,
                        paneAddress,
                        ClientAbilityParser.SkillPaneSnapshotSize,
                        out var bytes))
                {
                    skillbook = null;
                    return false;
                }

                var record = ClientAbilityParser.ParseSkillPane(bytes);
                if (record.Slot is <= 0 or > SkillSnapshot.MaximumSlot ||
                    !slots.Add(record.Slot))
                {
                    skillbook = null;
                    return false;
                }

                skills.Add(
                    ClientAbilityParser.CreateSkill(
                        record,
                        abilityCatalog));
            }

            if (skills.Count == 0 ||
                !TryValidatePaneTable(
                    reader,
                    SkillbookPanesKey,
                    SkillbookPaneCapacityKey,
                    observation))
            {
                skillbook = null;
                return false;
            }

            skillbook = ClientAbilityParser.CreateSkillbook(skills);
            return true;
        }
        catch (InvalidDataException)
        {
            skillbook = null;
            return false;
        }
    }

    private bool TryReadSpellbookPanes(
        MappedMemoryReader reader,
        out SpellbookSnapshot? spellbook)
    {
        if (!TryReadPaneTable(
                reader,
                SpellbookPanesKey,
                SpellbookPaneCapacityKey,
                out var observation))
        {
            spellbook = null;
            return false;
        }

        try
        {
            var spells = new List<SpellSnapshot>(observation.PointerCount);
            var slots = new HashSet<int>();
            foreach (var paneAddress in observation.PaneAddresses)
            {
                if (paneAddress.IsNull)
                {
                    continue;
                }

                if (!TryReadPaneSnapshot(
                        reader.Session,
                        paneAddress,
                        ClientAbilityParser.SpellPaneSnapshotSize,
                        out var bytes))
                {
                    spellbook = null;
                    return false;
                }

                var record = ClientAbilityParser.ParseSpellPane(bytes);
                if (record.Slot is <= 0 or > SpellSnapshot.MaximumSlot ||
                    !slots.Add(record.Slot))
                {
                    spellbook = null;
                    return false;
                }

                spells.Add(
                    ClientAbilityParser.CreateSpell(
                        record,
                        abilityCatalog));
            }

            if (spells.Count == 0 ||
                !TryValidatePaneTable(
                    reader,
                    SpellbookPanesKey,
                    SpellbookPaneCapacityKey,
                    observation))
            {
                spellbook = null;
                return false;
            }

            spellbook = ClientAbilityParser.CreateSpellbook(spells);
            return true;
        }
        catch (InvalidDataException)
        {
            spellbook = null;
            return false;
        }
    }

    private static bool TryReadPaneTable(
        MappedMemoryReader reader,
        string panesKey,
        string capacityKey,
        out PaneTableObservation observation)
    {
        if (!reader.TryResolveAddress(
                capacityKey,
                out var capacityAddress,
                out _) ||
            !reader.Session.TryReadInt32(
                capacityAddress,
                out var capacity,
                out _) ||
            capacity is <= 0 or > ClientAbilityParser.PaneRecordCount ||
            !reader.TryResolveAddress(
                panesKey,
                out var pointerTableAddress,
                out _))
        {
            observation = default;
            return false;
        }

        var definition = reader.Map.Find(panesKey)!;
        var pointerCount = Math.Min(capacity, definition.Capacity);
        var pointers = new byte[
            checked(
                pointerCount *
                ClientAbilityParser.PanePointerSize)];
        if (!reader.Session.TryRead(
                pointerTableAddress,
                pointers,
                out _))
        {
            observation = default;
            return false;
        }

        var paneAddresses = new MemoryAddress[pointerCount];
        for (var index = 0; index < pointerCount; index++)
        {
            paneAddresses[index] = new MemoryAddress(
                BinaryPrimitives.ReadUInt32LittleEndian(
                    pointers.AsSpan(
                        index * ClientAbilityParser.PanePointerSize,
                        ClientAbilityParser.PanePointerSize)));
        }

        observation = new PaneTableObservation(
            capacity,
            capacityAddress,
            pointerTableAddress,
            pointers,
            paneAddresses);
        return true;
    }

    private static bool TryValidatePaneTable(
        MappedMemoryReader reader,
        string panesKey,
        string capacityKey,
        PaneTableObservation expected)
    {
        if (!reader.TryResolveAddress(
                capacityKey,
                out var capacityAddress,
                out _) ||
            capacityAddress != expected.CapacityAddress ||
            !reader.Session.TryReadInt32(
                capacityAddress,
                out var capacity,
                out _) ||
            capacity != expected.Capacity ||
            !reader.TryResolveAddress(
                panesKey,
                out var pointerTableAddress,
                out _) ||
            pointerTableAddress != expected.PointerTableAddress)
        {
            return false;
        }

        var pointers = new byte[expected.Pointers.Length];
        return reader.Session.TryRead(
                pointerTableAddress,
                pointers,
                out _) &&
            pointers.AsSpan().SequenceEqual(expected.Pointers);
    }

    private static bool TryReadPaneSnapshot(
        MemoryReadSession session,
        MemoryAddress paneAddress,
        int snapshotSize,
        out byte[] snapshot)
    {
        if (!paneAddress.TryOffset(
                ClientAbilityParser.PaneSnapshotOffset,
                out var snapshotAddress))
        {
            snapshot = [];
            return false;
        }

        snapshot = new byte[snapshotSize];
        return session.TryRead(snapshotAddress, snapshot, out _);
    }

    private readonly record struct PaneTableObservation(
        int Capacity,
        MemoryAddress CapacityAddress,
        MemoryAddress PointerTableAddress,
        byte[] Pointers,
        MemoryAddress[] PaneAddresses)
    {
        public int PointerCount => PaneAddresses.Length;
    }
}
