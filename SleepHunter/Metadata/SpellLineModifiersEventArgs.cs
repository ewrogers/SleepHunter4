using System;

namespace SleepHunter.Metadata
{
    internal delegate void SpellLineModifiersEventHandler(object sender, SpellLineModifiersEventArgs e);

    internal sealed class SpellLineModifiersEventArgs : EventArgs
    {
        private readonly SpellLineModifiers modifiers;

        public SpellLineModifiers Modifiers => modifiers;

        public SpellLineModifiersEventArgs(SpellLineModifiers modifiers)
        {
            this.modifiers = modifiers;
        }
    }
}
