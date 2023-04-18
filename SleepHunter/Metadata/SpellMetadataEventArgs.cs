using System;

namespace SleepHunter.Metadata
{
    internal delegate void SpellMetadataEventHandler(object sender, SpellMetadataEventArgs e);

    internal sealed class SpellMetadataEventArgs : EventArgs
    {
        readonly SpellMetadata spell;

        public SpellMetadata Spell
        {
            get { return spell; }
        }

        public SpellMetadataEventArgs(SpellMetadata spell)
        {
            this.spell = spell ?? throw new ArgumentNullException(nameof(spell));
        }
    }
}
