using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SleepHunter.Data
{
   public sealed class InventoryItem : NotifyObject
   {
      bool isEmpty;
      int slot;
      int iconIndex;
      string name;

      public bool IsEmpty
      {
         get { return isEmpty; }
         set { SetProperty(ref isEmpty, value, "IsEmpty"); }
      }

      public int Slot
      {
         get { return slot; }
         set { SetProperty(ref slot, value, "Slot"); }
      }

      public int IconIndex
      {
         get { return iconIndex; }
         set { SetProperty(ref iconIndex, value, "IconIndex"); }
      }

      public string Name
      {
         get { return name; }
         set { SetProperty(ref name, value, "Name"); }
      }

      private InventoryItem()
      {

      }

      public InventoryItem(int slot, string name, int iconIndex = 0)
      {
         this.slot = slot;
         this.name = name;
         this.iconIndex = iconIndex;

         this.isEmpty = false;
      }

      public override string ToString()
      {
         return this.Name ?? "Unknown Item";
      }

      public static InventoryItem MakeEmpty(int slot)
      {
         return new InventoryItem { Slot = slot, IsEmpty = true };
      }
   }
}
