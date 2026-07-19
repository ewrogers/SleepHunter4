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

        public bool IsEquipped(string itemName, EquipmentSlot slot)
        {
            CheckIfDisposed();

            itemName = itemName.Trim();
            var isEquipped = true;

            if (slot.HasFlag(EquipmentSlot.Accessory1))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Accessory1).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Accessory2))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Accessory2).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Accessory3))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Accessory3).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Armor))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Armor).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Belt))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Belt).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Boots))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Boots).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Earring))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Earring).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Greaves))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Greaves).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Hat))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Hat).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Helmet))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Helmet).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.LeftGauntlet))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.LeftGauntlet).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.LeftRing))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.LeftRing).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Necklace))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Necklace).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Overcoat))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Overcoat).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.RightGauntlet))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.RightGauntlet).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.RightRing))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.RightRing).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Shield))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Shield).Name,
                    StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Weapon))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Weapon).Name,
                    StringComparison.OrdinalIgnoreCase);

            return isEquipped;
        }

        public bool IsEmpty(EquipmentSlot slot)
        {
            CheckIfDisposed();

            var isEmpty = true;

            if (slot.HasFlag(EquipmentSlot.Accessory1))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Accessory1);

            if (slot.HasFlag(EquipmentSlot.Accessory2))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Accessory2);

            if (slot.HasFlag(EquipmentSlot.Accessory3))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Accessory3);

            if (slot.HasFlag(EquipmentSlot.Armor))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Armor);

            if (slot.HasFlag(EquipmentSlot.Belt))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Belt);

            if (slot.HasFlag(EquipmentSlot.Boots))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Boots);

            if (slot.HasFlag(EquipmentSlot.Earring))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Earring);

            if (slot.HasFlag(EquipmentSlot.Greaves))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Greaves);

            if (slot.HasFlag(EquipmentSlot.Hat))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Hat);

            if (slot.HasFlag(EquipmentSlot.Helmet))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Helmet);

            if (slot.HasFlag(EquipmentSlot.LeftGauntlet))
                isEmpty &= IsSlotEmpty(EquipmentSlot.LeftGauntlet);

            if (slot.HasFlag(EquipmentSlot.LeftRing))
                isEmpty &= IsSlotEmpty(EquipmentSlot.LeftRing);

            if (slot.HasFlag(EquipmentSlot.Necklace))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Necklace);

            if (slot.HasFlag(EquipmentSlot.Overcoat))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Overcoat);

            if (slot.HasFlag(EquipmentSlot.RightGauntlet))
                isEmpty &= IsSlotEmpty(EquipmentSlot.RightGauntlet);

            if (slot.HasFlag(EquipmentSlot.RightRing))
                isEmpty &= IsSlotEmpty(EquipmentSlot.RightRing);

            if (slot.HasFlag(EquipmentSlot.Shield))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Shield);

            if (slot.HasFlag(EquipmentSlot.Weapon))
                isEmpty &= IsSlotEmpty(EquipmentSlot.Weapon);

            return isEmpty;
        }

        public InventoryItem GetSlot(EquipmentSlot slot)
        {
            CheckIfDisposed();
            return equipment[(int)slot];
        }

        private bool IsSlotEmpty(EquipmentSlot slot)
        {
            var item = GetSlot(slot);

            var isEmpty = item == null || item.IsEmpty;
            return isEmpty;
        }

        protected override void OnUpdate()
        {
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            if (TryUpdateFromSnapshot(version))
                return;

            if (!version.TryGetVariable(EquipmentKey, out var equipmentVariable))
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
                equipment[i].Quantity = isEmpty ? 0 : 1;
                equipment[i].Durability = 0;
                equipment[i].MaximumDurability = 0;
                equipment[i].Icon = null;
            }

            ResetDefaults(entryCount);
        }

        private bool TryUpdateFromSnapshot(Settings.ClientVersion version)
        {
            if (!version.TryGetVariable(EquipmentSnapshotKey, out var snapshotVariable) ||
                !snapshotVariable.TryDereferenceValue(reader, out var snapshotAddress))
            {
                return false;
            }

            stream.Position = snapshotAddress;
            var snapshot = reader.ReadBytes(EquipmentSnapshotSize);
            if (snapshot.Length != EquipmentSnapshotSize)
                throw new EndOfStreamException("The equipment snapshot was incomplete.");

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
                var durability = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                    snapshot.Slice(durabilityOffset, sizeof(uint)));
                var maximumDurability = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
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
