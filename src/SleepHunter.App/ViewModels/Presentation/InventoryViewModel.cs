using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Media;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels.Presentation
{
    public sealed class InventoryViewModel :
        ObservableObject,
        IEnumerable<InventoryItemViewModel>
    {
        private const int GoldIconIndex = 136;

        public const int InventoryCount = 60;

        private readonly InventoryItemViewModel[] inventory =
            new InventoryItemViewModel[InventoryCount];
        private readonly IconManager icons;
        private int gold;

        public IEnumerable<InventoryItemViewModel> ItemsAndGold =>
            inventory;

        public int Gold
        {
            get => gold;
            private set
            {
                if (SetProperty(ref gold, value))
                    UpdateGoldInventoryItem();
            }
        }

        public InventoryViewModel(IconManager icons = null)
        {
            this.icons = icons;

            for (var index = 0; index < inventory.Length; index++)
                inventory[index] =
                    InventoryItemViewModel.MakeEmpty(index + 1);

            UpdateGoldInventoryItem();
        }

        internal void Apply(
            InventorySnapshot snapshot,
            uint observedGold)
        {
            var items = snapshot?.Items ?? [];
            var bySlot = items.ToDictionary(item => item.Slot);

            for (var slot = 1; slot < InventoryCount; slot++)
            {
                var item = inventory[slot - 1];
                if (!bySlot.TryGetValue(slot, out var observed))
                {
                    ResetItem(item);
                    continue;
                }

                item.IsEmpty = false;
                item.IconIndex = observed.Sprite;
                item.IsGold = false;
                item.Name = observed.Name;
                item.Quantity = ToDisplayValue(observed.Quantity);
                item.Durability = observed.CurrentDurability;
                item.MaximumDurability =
                    observed.MaximumDurability;
                item.Icon = icons?.GetInventoryItemIcon(
                        observed.Sprite,
                        observed.DyeColor);
            }

            Gold = ToDisplayValue(observedGold);
        }

        internal void Reset() => Apply(
            InventorySnapshot.Empty,
            observedGold: 0);

        public IEnumerator<InventoryItemViewModel> GetEnumerator()
        {
            foreach (var item in inventory)
            {
                if (!item.IsEmpty)
                    yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void UpdateGoldInventoryItem()
        {
            var item = inventory[^1];
            item.IsEmpty = false;
            item.IconIndex = GoldIconIndex;
            item.IsGold = true;
            item.Name = "Gold";
            item.Quantity = Gold;
            item.Durability = 0;
            item.MaximumDurability = 0;
            item.Icon = icons?.GetInventoryItemIcon(
                GoldIconIndex);
        }

        private static void ResetItem(
            InventoryItemViewModel item)
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

        private static int ToDisplayValue(uint value) =>
            value > int.MaxValue
                ? int.MaxValue
                : (int)value;
    }
}
