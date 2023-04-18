using System;

namespace SleepHunter.Metadata
{
    public delegate void SpellLineModifiersEventHandler(object sender, SpellLineModifiersEventArgs e);

    public sealed class SpellLineModifiersEventArgs : EventArgs
    {
        public SpellLineModifiers Modifiers { get; }

        public SpellLineModifiersEventArgs(SpellLineModifiers modifiers)
            => Modifiers = modifiers ?? throw new ArgumentNullException(nameof(modifiers));
    }
}
