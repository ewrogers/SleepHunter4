using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SleepHunter.Common;
using SleepHunter.Extensions;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    
    public sealed class EquipmentSet : UpdatableObject, IEnumerable<InventoryItem>
    {
        private const string EquipmentKey = @"Equipment";
        public const int EquipmentCount = 18;

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

            for (int i = 0; i < equipment.Length; i++)
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
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Accessory1).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Accessory2))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Accessory2).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Accessory3))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Accessory3).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Armor))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Armor).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Belt))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Belt).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Boots))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Boots).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Earring))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Earring).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Greaves))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Greaves).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Hat))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Hat).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Helmet))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Helmet).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.LeftGauntlet))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.LeftGauntlet).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.LeftRing))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.LeftRing).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Necklace))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Necklace).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Overcoat))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Overcoat).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.RightGauntlet))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.RightGauntlet).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.RightRing))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.RightRing).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Shield))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Shield).Name, StringComparison.OrdinalIgnoreCase);

            if (slot.HasFlag(EquipmentSlot.Weapon))
                isEquipped &= string.Equals(itemName, GetSlot(EquipmentSlot.Weapon).Name, StringComparison.OrdinalIgnoreCase);

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

            for (int i = 0; i < entryCount; i++)
            {
                try
                {
                    var name = reader.ReadFixedString(equipmentVariable.MaxLength);
                    var isEmpty = string.IsNullOrWhiteSpace(name);

                    equipment[i].IsEmpty = isEmpty;
                    equipment[i].IconIndex = 0;
                    equipment[i].Name = name.StripNumbers();
                    equipment[i].Quantity = isEmpty ? 0 : 1;
                }
                catch { }
            }
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
            for (int i = 0; i < equipment.Length; i++)
            {
                equipment[i].IsEmpty = true;
                equipment[i].Name = null;
                equipment[i].Quantity = 0;
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
