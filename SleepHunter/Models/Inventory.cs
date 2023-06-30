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
    public sealed class Inventory : UpdatableObject, IEnumerable<InventoryItem>
    {
        private const string InventoryKey = @"Inventory";
        private const string GoldKey = @"Gold";

        public static readonly int InventoryCount = 60;

        private readonly Stream stream;
        private readonly BinaryReader reader;
        private readonly InventoryItem[] inventory = new InventoryItem[InventoryCount];
        private int gold;

        public Player Owner { get; }

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

            for (int i = 0; i < inventory.Length; i++)
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

            // Gold is the last item, skip it
            for (int i = 0; i < entryCount - 1; i++)
            {
                try
                {
                    var hasItem = reader.ReadInt16() != 0;
                    var iconIndex = reader.ReadUInt16();
                    reader.ReadByte();
                    var name = reader.ReadFixedString(inventoryVariable.MaxLength);
                    reader.ReadByte();

                    inventory[i].IsEmpty = !hasItem;
                    inventory[i].IconIndex = iconIndex;
                    inventory[i].Name = name.StripNumbers();
                }
                catch { }
            }

            UpdateGold();
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
            inventory[InventoryCount - 1].Name = $"Gold ({Gold:n0})";
        }

        public void ResetDefaults()
        {
            for (int i = 0; i < inventory.Length; i++)
            {
                inventory[i].IsEmpty = true;
                inventory[i].Name = null;
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
