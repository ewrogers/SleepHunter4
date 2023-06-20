using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.Extensions;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class Inventory : IEnumerable<InventoryItem>
    {
        private const string InventoryKey = @"Inventory";

        public static readonly int InventoryCount = 60;

        private readonly List<InventoryItem> inventory = new(InventoryCount);

        public event EventHandler InventoryUpdated;

        public Player Owner { get; }

        public int Count => inventory.Count((item) => { return !item.IsEmpty; });

        public IEnumerable<string> ItemNames => 
            from i in inventory where !i.IsEmpty && !string.IsNullOrWhiteSpace(i.Name) select i.Name;

        public Inventory(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            InitializeInventory();
        }

        private void InitializeInventory()
        {
            inventory.Clear();

            for (int i = 0; i < inventory.Capacity; i++)
                inventory.Add(InventoryItem.MakeEmpty(i + 1));
        }

        public InventoryItem GetItem(string itemName)
        {
            itemName = itemName.Trim();

            foreach (var item in inventory)
                if (string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase))
                    return item;

            return null;
        }

        public int FindItemSlot(string itemName)
        {
            itemName = itemName.Trim();

            foreach (var item in inventory)
                if (string.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase))
                    return item.Slot;

            return -1;
        }

        public void Update()
        {
            Update(Owner.Accessor);
            InventoryUpdated?.Invoke(this, EventArgs.Empty);
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

            var inventoryVariable = version.GetVariable(InventoryKey);

            if (inventoryVariable == null)
            {
                ResetDefaults();
                return;
            }

            using var stream = accessor.GetStream();
            using var reader = new BinaryReader(stream, Encoding.ASCII);
            var inventoryPointer = inventoryVariable.DereferenceValue(reader);

            if (inventoryPointer == 0)
            {
                ResetDefaults();
                return;
            }

            reader.BaseStream.Position = inventoryPointer;
            

            for (int i = 0; i < inventoryVariable.Count; i++)
            {
                try
                {
                    bool hasItem = reader.ReadInt16() != 0;
                    ushort iconIndex = reader.ReadUInt16();
                    reader.ReadByte();
                    string name = reader.ReadFixedString(inventoryVariable.MaxLength);
                    reader.ReadByte();

                    inventory[i].IsEmpty = !hasItem;
                    inventory[i].IconIndex = iconIndex;
                    inventory[i].Name = name.StripNumbers();
                }
                catch { }
            }
        }

        public void ResetDefaults()
        {
            for (int i = 0; i < inventory.Capacity; i++)
            {
                inventory[i].IsEmpty = true;
                inventory[i].Name = null;
            }
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
