using System;

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
