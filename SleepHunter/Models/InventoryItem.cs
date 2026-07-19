using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Globalization;
using SleepHunter.Common;

namespace SleepHunter.Models
{
    public sealed class InventoryItem : ObservableObject
    {
        private static readonly Regex ColorTextRegex = new(@"{=[a-z]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private bool isEmpty;
        private int slot;
        private int iconIndex;
        private byte color;
        private bool isGold;
        private string name;
        private int quantity;
        private uint durability;
        private uint maximumDurability;
        private ImageSource icon;

        public bool IsEmpty
        {
            get => isEmpty;
            set => SetProperty(ref isEmpty, value);
        }

        public int Slot
        {
            get => slot;
            set => SetProperty(ref slot, value);
        }

        public int IconIndex
        {
            get => iconIndex;
            set => SetProperty(ref iconIndex, value, nameof(IconIndex),
                (_) => RaisePropertyChanged(nameof(SpriteNumber)));
        }

        public int SpriteNumber => IconIndex > 0x8000 ? IconIndex - 0x8000 : IconIndex;

        public byte Color
        {
            get => color;
            set => SetProperty(ref color, value);
        }

        public bool IsGold
        {
            get => isGold;
            set => SetProperty(ref isGold, value, nameof(IsGold),
                (_) =>
                {
                    RaisePropertyChanged(nameof(ShowsQuantity));
                    RaisePropertyChanged(nameof(QuantityBadgeText));
                });
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value, nameof(Name), (_) => RaisePropertyChanged(nameof(DisplayName)));
        }

        public string DisplayName => ColorTextRegex.Replace(Name ?? string.Empty, string.Empty);

        public int Quantity
        {
            get => quantity;
            set => SetProperty(ref quantity, value, nameof(Quantity),
                (_) =>
                {
                    RaisePropertyChanged(nameof(FormattedQuantity));
                    RaisePropertyChanged(nameof(ShowsQuantity));
                    RaisePropertyChanged(nameof(QuantityBadgeText));
                });
        }

        public string FormattedQuantity => Quantity.ToString("N0", CultureInfo.InvariantCulture);

        public string QuantityBadgeText => IsGold ? FormattedQuantity : $"x{FormattedQuantity}";

        public bool ShowsQuantity => IsGold || Quantity > 1;

        public uint Durability
        {
            get => durability;
            set => SetProperty(ref durability, value, nameof(Durability), (_) => RaiseDurabilityPropertiesChanged());
        }

        public uint MaximumDurability
        {
            get => maximumDurability;
            set => SetProperty(ref maximumDurability, value, nameof(MaximumDurability),
                (_) => RaiseDurabilityPropertiesChanged());
        }

        public bool HasDurability => MaximumDurability > 0;

        public string FormattedDurability =>
            $"{Durability.ToString(CultureInfo.InvariantCulture)} / " +
            MaximumDurability.ToString(CultureInfo.InvariantCulture);

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        private InventoryItem() { }

        public InventoryItem(int slot, string name, int iconIndex = 0, byte color = 0, int quantity = 1)
        {
            this.slot = slot;
            this.name = name;
            this.iconIndex = iconIndex;
            this.color = color;
            this.quantity = quantity;

            isEmpty = false;
        }

        public override string ToString() => Name ?? "Unknown Item";

        public static InventoryItem MakeEmpty(int slot) => new() { Slot = slot, IsEmpty = true, Quantity = 0 };

        private void RaiseDurabilityPropertiesChanged()
        {
            RaisePropertyChanged(nameof(HasDurability));
            RaisePropertyChanged(nameof(FormattedDurability));
        }
    }
}
