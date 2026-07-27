using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SleepHunter.ViewModels.Presentation
{
    public sealed class InventoryItemViewModel : ObservableObject
    {
        private static readonly Regex ColorTextRegex = new(@"{=[a-z]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private bool isEmpty;
        private int slot;
        private int iconIndex;
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
            set
            {
                if (SetProperty(ref iconIndex, value))
                    OnPropertyChanged(nameof(SpriteNumber));
            }
        }

        public int SpriteNumber => IconIndex > 0x8000 ? IconIndex - 0x8000 : IconIndex;

        public bool IsGold
        {
            get => isGold;
            set
            {
                if (!SetProperty(ref isGold, value))
                    return;

                OnPropertyChanged(nameof(ShowsQuantity));
                OnPropertyChanged(nameof(QuantityBadgeText));
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (SetProperty(ref name, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName => ColorTextRegex.Replace(Name ?? string.Empty, string.Empty);

        public int Quantity
        {
            get => quantity;
            set
            {
                if (!SetProperty(ref quantity, value))
                    return;

                OnPropertyChanged(nameof(FormattedQuantity));
                OnPropertyChanged(nameof(ShowsQuantity));
                OnPropertyChanged(nameof(QuantityBadgeText));
            }
        }

        public string FormattedQuantity => Quantity.ToString("N0", CultureInfo.InvariantCulture);

        public string QuantityBadgeText => IsGold
            ? FormatCompactGoldQuantity(Quantity)
            : $"x{FormattedQuantity}";

        public bool ShowsQuantity => IsGold || Quantity > 1;

        public uint Durability
        {
            get => durability;
            set
            {
                if (SetProperty(ref durability, value))
                    RaiseDurabilityPropertiesChanged();
            }
        }

        public uint MaximumDurability
        {
            get => maximumDurability;
            set
            {
                if (SetProperty(ref maximumDurability, value))
                    RaiseDurabilityPropertiesChanged();
            }
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

        private InventoryItemViewModel() { }

        public InventoryItemViewModel(
            int slot,
            string name,
            int iconIndex = 0,
            int quantity = 1)
        {
            this.slot = slot;
            this.name = name;
            this.iconIndex = iconIndex;
            this.quantity = quantity;

            isEmpty = false;
        }

        public override string ToString() => Name ?? "Unknown Item";

        public static InventoryItemViewModel MakeEmpty(int slot) =>
            new()
            {
                Slot = slot,
                IsEmpty = true,
                Quantity = 0
            };

        private void RaiseDurabilityPropertiesChanged()
        {
            OnPropertyChanged(nameof(HasDurability));
            OnPropertyChanged(nameof(FormattedDurability));
        }

        private static string FormatCompactGoldQuantity(int quantity)
        {
            const int thousands = 1_000;
            const int millions = 1_000_000;

            if (quantity < thousands)
                return quantity.ToString(CultureInfo.InvariantCulture);

            var divisor = quantity >= millions ? millions : thousands;
            var suffix = quantity >= millions ? "m" : "k";
            var scaledQuantity = System.Math.Round(
                quantity / (double)divisor,
                1,
                System.MidpointRounding.AwayFromZero);

            if (divisor == thousands && scaledQuantity >= thousands)
            {
                scaledQuantity = System.Math.Round(
                    quantity / (double)millions,
                    1,
                    System.MidpointRounding.AwayFromZero);
                suffix = "m";
            }

            return scaledQuantity.ToString("0.#", CultureInfo.InvariantCulture) + suffix;
        }
    }
}
