using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Metadata
{
   public delegate void SpellLineModifiersEventHandler(object sender, SpellLineModifiersEventArgs e);

   public sealed class SpellLineModifiersEventArgs : EventArgs
   {
      readonly SpellLineModifiers modifiers;

      public SpellLineModifiers Modifiers { get { return modifiers; } }

      public SpellLineModifiersEventArgs(SpellLineModifiers modifiers)
      {
         this.modifiers = modifiers;
      }
   }
}
