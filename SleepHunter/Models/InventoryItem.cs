using System.Text.RegularExpressions;
using System.Windows.Media;
using SleepHunter.Common;

namespace SleepHunter.Models
{
    public sealed class InventoryItem : ObservableObject
    {
        private static readonly Regex ColorTextRegex = new(@"{=[a-z]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private bool isEmpty;
        private int slot;
        private int iconIndex;
        private string name;
        private int quantity;
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
            set => SetProperty(ref iconIndex, value);
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
            set => SetProperty(ref quantity, value);
        }

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        private InventoryItem() { }

        public InventoryItem(int slot, string name, int iconIndex = 0, int quantity = 1)
        {
            this.slot = slot;
            this.name = name;
            this.iconIndex = iconIndex;
            this.quantity = quantity;

            isEmpty = false;
        }

        public override string ToString() => Name ?? "Unknown Item";

        public static InventoryItem MakeEmpty(int slot) => new() { Slot = slot, IsEmpty = true, Quantity = 0 };
    }
}
