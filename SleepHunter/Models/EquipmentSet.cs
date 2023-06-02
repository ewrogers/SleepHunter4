using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.Extensions;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public enum EquipmentSlot : byte
    {
        Weapon,
        Armor,
        Shield,
        Helmet,
        Earring,
        Necklace,
        LeftRing,
        RightRing,
        LeftGauntlet,
        RightGauntlet,
        Belt,
        Greaves,
        Boots,
        Accessory1,
        Overcoat,
        Hat,
        Accessory2,
        Accessory3
    }

    public sealed class EquipmentSet : IEnumerable<InventoryItem>
    {
        const string EquipmentKey = @"Equipment";

        public const int EquipmentCount = 18;

        Player owner;
        readonly List<InventoryItem> equipment = new(EquipmentCount);

        public event EventHandler EquipmentUpdated;

        public Player Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        public int Count { get { return equipment.Count((item) => { return !item.IsEmpty; }); } }

        public EquipmentSet()
           : this(null) { }

        public EquipmentSet(Player owner)
        {
            this.owner = owner;
            InitializeEquipment();
        }

        void InitializeEquipment()
        {
            for (int i = 0; i < EquipmentCount; i++)
            {
                var item = InventoryItem.MakeEmpty(i);
                equipment.Add(item);
            }
        }

        public bool IsEquipped(string itemName, EquipmentSlot slot)
        {
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

        bool IsSlotEmpty(EquipmentSlot slot)
        {
            var item = GetSlot(slot);

            var isEmpty = item == null || item.IsEmpty;
            return isEmpty;
        }

        public void Update()
        {
            if (owner == null)
                throw new InvalidOperationException("Player owner is null, cannot update.");

            Update(owner.Accessor);
            EquipmentUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Update(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            var version = this.Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            var equipmentVariable = version.GetVariable(EquipmentKey);

            if (equipmentVariable == null)
            {
                ResetDefaults();
                return;
            }

            Stream stream = null;
            try
            {

                stream = accessor.GetStream();
                using (var reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    stream = null;
                    var equipmentPointer = equipmentVariable.DereferenceValue(reader);

                    if (equipmentPointer == 0)
                    {
                        ResetDefaults();
                        return;
                    }

                    reader.BaseStream.Position = equipmentPointer;

                    for (int i = 0; i < equipmentVariable.Count; i++)
                    {
                        try
                        {
                            string name = reader.ReadFixedString(equipmentVariable.MaxLength);

                            equipment[i].IsEmpty = string.IsNullOrWhiteSpace(name);
                            equipment[i].IconIndex = 0;
                            equipment[i].Name = name.StripNumbers();
                        }
                        catch { }
                    }
                }
            }
            finally { stream?.Dispose(); }
        }

        public void ResetDefaults()
        {
            for (int i = 0; i < equipment.Capacity; i++)
            {
                equipment[i].IsEmpty = true;
                equipment[i].Name = null;
            }
        }

        public InventoryItem GetSlot(EquipmentSlot slot)
        {
            return equipment[(int)slot];
        }

        #region IEnumerable Methods
        public IEnumerator<InventoryItem> GetEnumerator()
        {
            foreach (var gear in equipment)
                if (!gear.IsEmpty)
                    yield return gear;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion
    }
}
