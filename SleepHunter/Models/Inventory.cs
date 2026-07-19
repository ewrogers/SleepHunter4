using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SleepHunter.Common;
using SleepHunter.IO.Process;
using SleepHunter.Media;

namespace SleepHunter.Models
{
    public sealed class Inventory : UpdatableObject, IEnumerable<InventoryItem>
    {
        private const string InventoryKey = @"Inventory";
        private const string InventoryPanesKey = @"InventoryPanes";
        private const string GoldKey = @"Gold";
        private const int GoldIconIndex = 136;
        internal const int InventoryRecordSize = 0x106;
        internal const int InventoryPaneSnapshotSize = 0xB5;

        public static readonly int InventoryCount = 60;

        private readonly Stream stream;
        private readonly BinaryReader reader;
        private readonly InventoryItem[] inventory = new InventoryItem[InventoryCount];
        private int gold;

        public Player Owner { get; init; }

        public IEnumerable<InventoryItem> ItemsAndGold => inventory;

        public int Gold
        {
            get => gold;
            set => SetProperty(ref gold, value, nameof(Gold), (_) =>
            {
                UpdateGoldInventoryItem();
                RaisePropertyChanged(nameof(ItemsAndGold));
            });
        }

        public IEnumerable<string> ItemNames => 
            from i in inventory where !i.IsEmpty && !string.IsNullOrWhiteSpace(i.Name) select i.Name;

        public Inventory(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);

            for (var i = 0; i < inventory.Length; i++)
                inventory[i] = InventoryItem.MakeEmpty(i + 1);

            UpdateGoldInventoryItem();
        }

        public InventoryItem GetItem(string itemName)
        {
            CheckIfDisposed();

            itemName = itemName.Trim();

            foreach (var item in inventory)
                if (string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase))
                    return item;

            return null;
        }

        public int FindItemSlot(string itemName)
        {
            CheckIfDisposed();

            itemName = itemName.Trim();

            foreach (var item in inventory)
                if (string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase))
                    return item.Slot;

            return -1;
        }

        protected override void OnUpdate()
        {
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            if (!version.TryGetVariable(InventoryKey, out var inventoryVariable))
            {
                ResetDefaults();
                return;
            }

            if (!inventoryVariable.TryDereferenceValue(reader, out var basePointer))
            {
                ResetDefaults();
                return;
            }

            stream.Position = basePointer;

            var entryCount = Math.Min(inventory.Length, inventoryVariable.Count);
            var snapshotLength = checked(entryCount * InventoryRecordSize);
            var snapshot = reader.ReadBytes(snapshotLength);
            if (snapshot.Length != snapshotLength)
                throw new EndOfStreamException("The inventory snapshot was incomplete.");

            var records = ParseInventorySnapshot(snapshot, entryCount);
            var paneRecords = ReadInventoryPaneRecords(version, records);

            // Gold is the last item, skip it
            for (var i = 0; i < entryCount - 1; i++)
            {
                var record = records[i];
                inventory[i].IsEmpty = !record.IsPresent;
                inventory[i].IconIndex = record.RawSprite;
                inventory[i].Color = record.DyeColor;
                inventory[i].IsGold = false;
                inventory[i].Name = record.Name;
                inventory[i].Quantity = record.IsPresent
                    ? GetQuantity(record, paneRecords[i])
                    : 0;
                inventory[i].MaximumDurability = HasMatchingPaneRecord(record, paneRecords[i])
                    ? paneRecords[i].MaximumDurability
                    : 0;
                inventory[i].Durability = HasMatchingPaneRecord(record, paneRecords[i])
                    ? paneRecords[i].Durability
                    : 0;
                inventory[i].Icon = record.IsPresent
                    ? IconManager.Instance.GetInventoryItemIcon(record.RawSprite, record.DyeColor)
                    : null;
            }

            UpdateGold();
        }

        private InventoryPaneRecord[] ReadInventoryPaneRecords(
            Settings.ClientVersion version,
            IReadOnlyList<InventoryRecord> inventoryRecords)
        {
            var paneRecords = new InventoryPaneRecord[inventoryRecords.Count];
            if (!version.TryGetVariable(InventoryPanesKey, out var panesVariable) ||
                !panesVariable.TryDereferenceValue(reader, out var panePointersAddress))
            {
                return paneRecords;
            }

            try
            {
                stream.Position = panePointersAddress;
                var pointers = reader.ReadBytes(checked(inventoryRecords.Count * sizeof(uint)));
                if (pointers.Length != inventoryRecords.Count * sizeof(uint))
                    return paneRecords;

                for (var index = 0; index < inventoryRecords.Count; index++)
                {
                    if (!inventoryRecords[index].IsPresent)
                        continue;

                    var paneAddress = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                        pointers.AsSpan(index * sizeof(uint), sizeof(uint)));
                    if (paneAddress == 0)
                        continue;

                    try
                    {
                        stream.Position = paneAddress + 0x190;
                        var paneSnapshot = reader.ReadBytes(InventoryPaneSnapshotSize);
                        if (paneSnapshot.Length == InventoryPaneSnapshotSize)
                            paneRecords[index] = ParseInventoryPaneSnapshot(paneSnapshot);
                    }
                    catch
                    {
                        // Keep the compact-record fallback for this slot.
                    }
                }
            }
            catch
            {
                // The live pane is optional; compact inventory records remain usable without it.
            }

            return paneRecords;
        }

        private static int GetQuantity(InventoryRecord inventoryRecord, InventoryPaneRecord paneRecord)
        {
            if (!HasMatchingPaneRecord(inventoryRecord, paneRecord) || paneRecord.Quantity == 0)
                return 1;

            return paneRecord.Quantity > int.MaxValue ? int.MaxValue : (int)paneRecord.Quantity;
        }

        private static bool HasMatchingPaneRecord(InventoryRecord inventoryRecord, InventoryPaneRecord paneRecord) =>
            paneRecord.IsValid && paneRecord.RawSprite == inventoryRecord.RawSprite;

        internal static InventoryRecord[] ParseInventorySnapshot(ReadOnlySpan<byte> snapshot, int recordCount)
        {
            if (recordCount < 0)
                throw new ArgumentOutOfRangeException(nameof(recordCount));

            var expectedLength = checked(recordCount * InventoryRecordSize);
            if (snapshot.Length != expectedLength)
                throw new InvalidDataException(
                    $"An inventory snapshot with {recordCount} records must contain {expectedLength} bytes.");

            var records = new InventoryRecord[recordCount];
            for (var index = 0; index < recordCount; index++)
            {
                var record = snapshot.Slice(index * InventoryRecordSize, InventoryRecordSize);
                var nameBytes = record.Slice(5, 256);
                var terminator = nameBytes.IndexOf((byte)0);
                if (terminator >= 0)
                    nameBytes = nameBytes[..terminator];

                records[index] = new InventoryRecord(
                    record[0] != 0,
                    System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(2, 2)),
                    record[4],
                    Encoding.ASCII.GetString(nameBytes));
            }

            return records;
        }

        internal readonly record struct InventoryRecord(
            bool IsPresent,
            ushort RawSprite,
            byte DyeColor,
            string Name);

        internal static InventoryPaneRecord ParseInventoryPaneSnapshot(ReadOnlySpan<byte> snapshot)
        {
            if (snapshot.Length != InventoryPaneSnapshotSize)
                throw new InvalidDataException(
                    $"An inventory pane snapshot must contain {InventoryPaneSnapshotSize} bytes.");

            return new InventoryPaneRecord(
                true,
                System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(snapshot),
                snapshot[0x82],
                System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(snapshot.Slice(0xAC, 4)),
                System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(snapshot.Slice(0xA8, 4)),
                System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(snapshot.Slice(0xB0, 4)),
                snapshot[0xB4] != 0);
        }

        internal readonly record struct InventoryPaneRecord(
            bool IsValid,
            ushort RawSprite,
            byte DyeColor,
            uint MaximumDurability,
            uint Durability,
            uint Quantity,
            bool CanStack);

        private void UpdateGold()
        {
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            if (!version.TryGetVariable(GoldKey, out var goldVariable))
            {
                Gold = 0;
                return;
            }

            if (goldVariable.TryReadUInt32(reader, out var goldValue))
                Gold = (int)goldValue;
            else
                Gold = 0;

            UpdateGoldInventoryItem();
        }

        private void UpdateGoldInventoryItem()
        {
            inventory[InventoryCount - 1].IsEmpty = false;
            inventory[InventoryCount - 1].IconIndex = GoldIconIndex;
            inventory[InventoryCount - 1].Color = 0;
            inventory[InventoryCount - 1].IsGold = true;
            inventory[InventoryCount - 1].Name = "Gold";
            inventory[InventoryCount - 1].Quantity = Gold;
            inventory[InventoryCount - 1].MaximumDurability = 0;
            inventory[InventoryCount - 1].Durability = 0;
            inventory[InventoryCount - 1].Icon = IconManager.Instance.GetItemIcon(GoldIconIndex);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void ResetDefaults()
        {
            for (var i = 0; i < inventory.Length; i++)
            {
                inventory[i].IsEmpty = true;
                inventory[i].IconIndex = 0;
                inventory[i].Color = 0;
                inventory[i].IsGold = false;
                inventory[i].Name = null;
                inventory[i].Quantity = 0;
                inventory[i].MaximumDurability = 0;
                inventory[i].Durability = 0;
                inventory[i].Icon = null;
            }

            Gold = 0;
        }

        public IEnumerator<InventoryItem> GetEnumerator()
        {
            foreach (var item in inventory)
                if (!item.IsEmpty)
                    yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
