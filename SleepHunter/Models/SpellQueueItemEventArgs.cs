using System;

namespace SleepHunter.Models
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
