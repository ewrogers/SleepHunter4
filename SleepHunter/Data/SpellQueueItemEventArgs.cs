using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Data
{
   public delegate void SpellQueueItemEventHandler(object sender, SpellQueueItemEventArgs e);

   public sealed class SpellQueueItemEventArgs : EventArgs
   {
      readonly SpellQueueItem spell;

      public SpellQueueItem Spell { get { return spell; } }

      public SpellQueueItemEventArgs(SpellQueueItem spell)
      {
         this.spell = spell;
      }
   }
}
