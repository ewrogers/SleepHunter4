using System;

namespace SleepHunter.Models
{
    public delegate void SpellQueueItemEventHandler(object sender, SpellQueueItemEventArgs e);

    public sealed class SpellQueueItemEventArgs : EventArgs
    {
        public SpellQueueItem Spell { get; }

        public SpellQueueItemEventArgs(SpellQueueItem spell)
            => Spell = spell ?? throw new ArgumentNullException(nameof(spell));
    }
}
