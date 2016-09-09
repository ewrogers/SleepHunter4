using System;

namespace SleepHunter.Metadata
{
    public delegate void SpellMetadataEventHandler(object sender, SpellMetadataEventArgs e);

   public sealed class SpellMetadataEventArgs : EventArgs
   {
      readonly SpellMetadata spell;

      public SpellMetadata Spell
      {
         get { return spell; }
      }

      public SpellMetadataEventArgs(SpellMetadata spell)
      {
         if (spell == null)
            throw new ArgumentNullException("spell");

         this.spell = spell;
      }
   }
}
