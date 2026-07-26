using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SleepHunter.Media;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Models
{
    public sealed class EquipmentSet :
        IEnumerable<InventoryItem>
    {
        public const int EquipmentCount = 18;

        private readonly InventoryItem[] equipment =
            new InventoryItem[EquipmentCount];

        public IEnumerable<InventoryItem> SortedBySlot =>
            equipment.OrderBy(item => item.Slot);

        public EquipmentSet()
        {
            for (var index = 0; index < equipment.Length; index++)
                equipment[index] = InventoryItem.MakeEmpty(index + 1);
        }

        internal void Apply(EquipmentSnapshot snapshot)
        {
            var items = snapshot?.Items ?? [];
            var bySlot = items.ToDictionary(item => item.Slot);

            for (var slot = 1; slot <= equipment.Length; slot++)
            {
                var item = equipment[slot - 1];
                if (!bySlot.TryGetValue(slot, out var observed))
                {
                    ResetItem(item);
                    continue;
                }

                item.IsEmpty = false;
                item.IconIndex = observed.Sprite;
                item.IsGold = false;
                item.Name = observed.Name;
                item.Quantity = 1;
                item.Durability = observed.CurrentDurability;
                item.MaximumDurability =
                    observed.MaximumDurability;
                item.Icon = IconManager.Instance
                    .GetInventoryItemIcon(
                        observed.Sprite,
                        observed.DyeColor);
            }
        }

        internal void Reset() => Apply(EquipmentSnapshot.Empty);

        public IEnumerator<InventoryItem> GetEnumerator()
        {
            foreach (var item in equipment)
            {
                if (!item.IsEmpty)
                    yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static void ResetItem(InventoryItem item)
        {
            item.IsEmpty = true;
            item.IconIndex = 0;
            item.IsGold = false;
            item.Name = null;
            item.Quantity = 0;
            item.Durability = 0;
            item.MaximumDurability = 0;
            item.Icon = null;
        }
    }
}
