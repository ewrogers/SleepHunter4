using System.Buffers.Binary;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

public sealed partial class ClientSnapshotCapture
{
    private static bool TryReadInventory(
        MappedMemoryReader reader,
        out InventorySnapshot? inventory,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        var definition = reader.Map.Find(InventoryKey)!;
        var length = checked(definition.RecordSize * definition.Capacity);
        if (!TryReadStableBlock(
                reader,
                InventoryKey,
                SnapshotSection.Inventory,
                length,
                out var bytes,
                out error,
                out failureQuality))
        {
            inventory = null;
            return false;
        }

        try
        {
            var compact = ClientInventoryParser.Parse(
                bytes,
                definition.Capacity);
            inventory = TryReadInventoryPanes(
                reader,
                compact,
                out var rich)
                ? rich
                : compact;
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }
        catch (InvalidDataException exception)
        {
            inventory = null;
            error = InvalidValue(
                SnapshotSection.Inventory,
                InventoryKey,
                exception.Message);
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }
    }

    private static bool TryReadInventoryPanes(
        MappedMemoryReader reader,
        InventorySnapshot compact,
        out InventorySnapshot inventory)
    {
        if (compact.Items.IsEmpty)
        {
            inventory = compact;
            return true;
        }

        if (!reader.TryResolveAddress(
                InventoryPanesKey,
                out var pointerTableAddress,
                out _))
        {
            inventory = compact;
            return false;
        }

        var definition = reader.Map.Find(InventoryPanesKey)!;
        var pointers = new byte[
            checked(
                definition.Capacity *
                ClientInventoryParser.PanePointerSize)];
        if (!reader.Session.TryRead(
                pointerTableAddress,
                pointers,
                out _))
        {
            inventory = compact;
            return false;
        }

        try
        {
            var items = new List<InventoryItemSnapshot>(
                compact.Items.Length);
            foreach (var compactItem in compact.Items)
            {
                var pointerOffset =
                    (compactItem.Slot - 1) *
                    ClientInventoryParser.PanePointerSize;
                var paneAddress = new MemoryAddress(
                    BinaryPrimitives.ReadUInt32LittleEndian(
                        pointers.AsSpan(
                            pointerOffset,
                            ClientInventoryParser.PanePointerSize)));
                if (paneAddress.IsNull ||
                    !paneAddress.TryOffset(
                        ClientInventoryParser.PaneSnapshotOffset,
                        out var snapshotAddress))
                {
                    inventory = compact;
                    return false;
                }

                var paneSnapshot = new byte[
                    ClientInventoryParser.PaneSnapshotSize];
                if (!reader.Session.TryRead(
                        snapshotAddress,
                        paneSnapshot,
                        out _))
                {
                    inventory = compact;
                    return false;
                }

                items.Add(
                    ClientInventoryParser.ParsePane(
                        paneSnapshot,
                        compactItem.Slot,
                        compactItem.Name,
                        compactItem.Sprite));
            }

            if (!reader.TryResolveAddress(
                    InventoryPanesKey,
                    out var currentPointerTableAddress,
                    out _) ||
                currentPointerTableAddress != pointerTableAddress)
            {
                inventory = compact;
                return false;
            }

            var currentPointers = new byte[pointers.Length];
            if (!reader.Session.TryRead(
                    currentPointerTableAddress,
                    currentPointers,
                    out _) ||
                !currentPointers.AsSpan().SequenceEqual(pointers))
            {
                inventory = compact;
                return false;
            }

            inventory = new InventorySnapshot(items);
            return true;
        }
        catch (InvalidDataException)
        {
            inventory = compact;
            return false;
        }
        catch (ArgumentException)
        {
            inventory = compact;
            return false;
        }
    }

    private static bool TryReadEquipment(
        MappedMemoryReader reader,
        out EquipmentSnapshot? equipment,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        var richDefinition = reader.Map.Find(EquipmentSnapshotKey)!;
        if (TryReadStableBlock(
                reader,
                EquipmentSnapshotKey,
                SnapshotSection.Equipment,
                richDefinition.RecordSize,
                out var richBytes,
                out _,
                out _) &&
            TryParseRichEquipment(
                richBytes,
                richDefinition.Capacity,
                out equipment))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        var compactDefinition = reader.Map.Find(EquipmentKey)!;
        var compactLength = checked(
            compactDefinition.MaximumLength *
            compactDefinition.Capacity);
        if (!TryReadStableBlock(
                reader,
                EquipmentKey,
                SnapshotSection.Equipment,
                compactLength,
                out var compactBytes,
                out error,
                out failureQuality))
        {
            equipment = null;
            return false;
        }

        try
        {
            equipment = ClientEquipmentParser.ParseCompact(
                compactBytes,
                compactDefinition.Capacity,
                compactDefinition.MaximumLength);
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }
        catch (InvalidDataException exception)
        {
            equipment = null;
            error = InvalidValue(
                SnapshotSection.Equipment,
                EquipmentKey,
                exception.Message);
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }
    }

    private static bool TryParseRichEquipment(
        ReadOnlySpan<byte> snapshot,
        int recordCount,
        out EquipmentSnapshot? equipment)
    {
        try
        {
            equipment = ClientEquipmentParser.ParseRich(
                snapshot,
                recordCount);
            return true;
        }
        catch (InvalidDataException)
        {
            equipment = null;
            return false;
        }
        catch (ArgumentException)
        {
            equipment = null;
            return false;
        }
    }

    private static bool TryReadStableBlock(
        MappedMemoryReader reader,
        string key,
        SnapshotSection section,
        int length,
        out byte[] bytes,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryResolveAddress(
                key,
                out var address,
                out var addressError))
        {
            bytes = [];
            error = MappingFailure(section, key, addressError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        bytes = new byte[length];
        if (!reader.Session.TryRead(
                address,
                bytes,
                out var memoryError))
        {
            error = MappingFailure(
                section,
                key,
                new MappedMemoryReadError(
                    MappedMemoryReadFailure.ValueReadFailed,
                    key,
                    ActualKind: MemoryValueKind.Binary,
                    MemoryError: memoryError));
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryResolveAddress(
                key,
                out var currentAddress,
                out addressError))
        {
            error = MappingFailure(section, key, addressError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (currentAddress != address)
        {
            error = StateChanged(
                section,
                key,
                $"The mapped block '{key}' changed roots during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        var confirmation = new byte[length];
        if (!reader.Session.TryRead(
                currentAddress,
                confirmation,
                out memoryError))
        {
            error = MappingFailure(
                section,
                key,
                new MappedMemoryReadError(
                    MappedMemoryReadFailure.ValueReadFailed,
                    key,
                    ActualKind: MemoryValueKind.Binary,
                    MemoryError: memoryError));
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!confirmation.AsSpan().SequenceEqual(bytes))
        {
            error = StateChanged(
                section,
                key,
                $"The mapped block '{key}' changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }
}
