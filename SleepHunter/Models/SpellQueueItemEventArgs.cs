using System;

namespace SleepHunter.Models
{
    internal delegate void SpellQueueItemEventHandler(object sender, SpellQueueItemEventArgs e);

    internal sealed class SpellQueueItemEventArgs : EventArgs
    {
        private readonly SpellQueueItem spell;

        public SpellQueueItem Spell => spell;

        public SpellQueueItemEventArgs(SpellQueueItem spell)
        {
            this.spell = spell;
        }
    }
}
