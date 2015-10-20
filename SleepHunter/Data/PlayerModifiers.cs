using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.IO.Process;

namespace SleepHunter.Data
{
   public sealed class PlayerModifiers : NotifyObject
   {
      Player owner;

      public Player Owner
      {
         get { return owner; }
         set { SetProperty(ref owner, value, "Owner"); }
      }

      public PlayerModifiers()
         : this(null) { }

      public PlayerModifiers(Player owner)
      {
         this.owner = owner;
      }

      public void Update()
      {
         if (owner == null)
            throw new InvalidOperationException("Player owner is null, cannot update.");

         Update(owner.Accessor);
      }

      public void Update(ProcessMemoryAccessor accessor)
      {
         if (accessor == null)
            throw new ArgumentNullException("accessor");
      }
   }
}
