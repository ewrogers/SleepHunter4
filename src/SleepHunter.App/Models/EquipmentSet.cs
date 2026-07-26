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

    public sealed class EquipmentSet : UpdatableObject, IEnumerable<InventoryItem>
    {
        private const string EquipmentKey = @"Equipment";
        private const string EquipmentSnapshotKey = @"EquipmentSnapshot";
        private const int SpriteArrayOffset = 0;
        private const int DyeArrayOffset = 0x24;
        private const int NameArrayOffset = 0x36;
        private const int EquipmentNameLength = 128;
        private const int DurabilityArrayOffset = 0x938;
        private const int DurabilityRecordSize = 8;

        public const int EquipmentCount = 18;
        internal const int EquipmentSnapshotSize = 0x9C8;

        private readonly Stream stream;
        private readonly BinaryReader reader;
        private readonly InventoryItem[] equipment = new InventoryItem[EquipmentCount];

        public Player Owner { get; init; }

        public IEnumerable<InventoryItem> SortedBySlot => equipment.OrderBy(item => item.Slot);

        public EquipmentSet(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);

            for (var i = 0; i < equipment.Length; i++)
            {
                equipment[i] = InventoryItem.MakeEmpty(i);
                equipment[i].Slot = i + 1;
            }
        }

        protected override void OnUpdate()
        {
            var layout = Owner.Layout;

            if (layout == null)
            {
                ResetDefaults();
                return;
            }

            if (TryUpdateFromSnapshot(layout))
                return;

            if (!layout.TryGetVariable(EquipmentKey, out var equipmentVariable))
            {
                ResetDefaults();
                return;
            }

            if (!equipmentVariable.TryDereferenceValue(reader, out var basePointer))
            {
                ResetDefaults();
                return;
            }

            stream.Position = basePointer;

            var entryCount = Math.Min(equipment.Length, equipmentVariable.Count);
            var snapshotLength = checked(entryCount * equipmentVariable.MaxLength);
            var snapshot = reader.ReadBytes(snapshotLength);
            if (snapshot.Length != snapshotLength)
                throw new EndOfStreamException("The equipment-name snapshot was incomplete.");

            for (var i = 0; i < entryCount; i++)
            {
                var name = ReadNullTerminatedAscii(
                    snapshot.AsSpan(i * equipmentVariable.MaxLength, equipmentVariable.MaxLength));
                var isEmpty = string.IsNullOrWhiteSpace(name);

                equipment[i].IsEmpty = isEmpty;
                equipment[i].IconIndex = 0;
                equipment[i].Color = 0;
                equipment[i].IsGold = false;
                equipment[i].Name = name;
                equipment[i].ClientDisplayName = name;
                equipment[i].CanStack = false;
                equipment[i].Quantity = isEmpty ? 0 : 1;
                equipment[i].Durability = 0;
                equipment[i].MaximumDurability = 0;
                equipment[i].Icon = null;
            }

            ResetDefaults(entryCount);
        }

        private bool TryUpdateFromSnapshot(
            Settings.ClientLayout layout)
        {
            if (!layout.TryGetVariable(EquipmentSnapshotKey, out var snapshotVariable) ||
                !snapshotVariable.TryDereferenceValue(reader, out var snapshotAddress))
            {
                return false;
            }

            stream.Position = snapshotAddress;
            var snapshot = reader.ReadBytes(EquipmentSnapshotSize);
            if (snapshot.Length != EquipmentSnapshotSize)
                throw new EndOfStreamException("The equipment snapshot was incomplete.");

            if (!snapshotVariable.TryDereferenceValue(reader, out var currentSnapshotAddress) ||
                currentSnapshotAddress != snapshotAddress)
            {
                return false;
            }

            var entryCount = Math.Min(equipment.Length, snapshotVariable.Count);
            var records = ParseEquipmentSnapshot(snapshot, entryCount);
            for (var index = 0; index < entryCount; index++)
            {
                var record = records[index];
                equipment[index].IsEmpty = !record.IsPresent;
                equipment[index].IconIndex = record.RawSprite;
                equipment[index].Color = record.DyeColor;
                equipment[index].IsGold = false;
                equipment[index].Name = record.Name;
                equipment[index].ClientDisplayName = record.Name;
                equipment[index].CanStack = false;
                equipment[index].Quantity = record.IsPresent ? 1 : 0;
                equipment[index].Durability = record.Durability;
                equipment[index].MaximumDurability = record.MaximumDurability;
                equipment[index].Icon = record.IsPresent
                    ? IconManager.Instance.GetInventoryItemIcon(record.RawSprite, record.DyeColor)
                    : null;
            }

            ResetDefaults(entryCount);
            return true;
        }

        internal static EquipmentRecord[] ParseEquipmentSnapshot(ReadOnlySpan<byte> snapshot, int recordCount)
        {
            if (recordCount < 0 || recordCount > EquipmentCount)
                throw new ArgumentOutOfRangeException(nameof(recordCount));

            if (snapshot.Length != EquipmentSnapshotSize)
                throw new InvalidDataException(
                    $"An equipment snapshot must contain {EquipmentSnapshotSize} bytes.");

            var records = new EquipmentRecord[recordCount];
            for (var index = 0; index < recordCount; index++)
            {
                var rawSprite = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(
                    snapshot.Slice(SpriteArrayOffset + index * sizeof(ushort), sizeof(ushort)));
                var dyeColor = snapshot[DyeArrayOffset + index];
                var name = ReadNullTerminatedAscii(
                    snapshot.Slice(NameArrayOffset + index * EquipmentNameLength, EquipmentNameLength));
                var durabilityOffset = DurabilityArrayOffset + index * DurabilityRecordSize;
                var maximumDurability = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                    snapshot.Slice(durabilityOffset, sizeof(uint)));
                var durability = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                    snapshot.Slice(durabilityOffset + sizeof(uint), sizeof(uint)));

                records[index] = new EquipmentRecord(
                    rawSprite,
                    dyeColor,
                    name,
                    durability,
                    maximumDurability);
            }

            return records;
        }

        internal readonly record struct EquipmentRecord(
            ushort RawSprite,
            byte DyeColor,
            string Name,
            uint Durability,
            uint MaximumDurability)
        {
            public bool IsPresent => RawSprite != 0 && !string.IsNullOrWhiteSpace(Name);
        }

        private static string ReadNullTerminatedAscii(ReadOnlySpan<byte> bytes)
        {
            var terminator = bytes.IndexOf((byte)0);
            if (terminator >= 0)
                bytes = bytes[..terminator];

            return Encoding.ASCII.GetString(bytes);
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

        private void ResetDefaults(int startIndex = 0)
        {
            for (var i = startIndex; i < equipment.Length; i++)
            {
                equipment[i].IsEmpty = true;
                equipment[i].IconIndex = 0;
                equipment[i].Color = 0;
                equipment[i].IsGold = false;
                equipment[i].Name = null;
                equipment[i].ClientDisplayName = null;
                equipment[i].CanStack = false;
                equipment[i].Quantity = 0;
                equipment[i].Durability = 0;
                equipment[i].MaximumDurability = 0;
                equipment[i].Icon = null;
            }
        }

        public IEnumerator<InventoryItem> GetEnumerator()
        {
            foreach (var gear in equipment)
                if (!gear.IsEmpty)
                    yield return gear;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
