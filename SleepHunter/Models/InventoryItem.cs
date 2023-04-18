using SleepHunter.Common;

namespace SleepHunter.Models
{
    public sealed class InventoryItem : ObservableObject
    {
        private bool isEmpty;
        private int slot;
        private int iconIndex;
        private string name;

        public bool IsEmpty
        {
            get { return isEmpty; }
            set { SetProperty(ref isEmpty, value); }
        }

        public int Slot
        {
            get { return slot; }
            set { SetProperty(ref slot, value); }
        }

        public int IconIndex
        {
            get { return iconIndex; }
            set { SetProperty(ref iconIndex, value); }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        private InventoryItem() { }

        public InventoryItem(int slot, string name, int iconIndex = 0)
        {
            this.slot = slot;
            this.name = name;
            this.iconIndex = iconIndex;

            isEmpty = false;
        }

        public override string ToString()
        {
            return Name ?? "Unknown Item";
        }

        public static InventoryItem MakeEmpty(int slot)
        {
            return new InventoryItem { Slot = slot, IsEmpty = true };
        }
    }
}
