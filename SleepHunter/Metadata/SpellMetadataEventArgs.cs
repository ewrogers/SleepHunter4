using System;

namespace SleepHunter.Metadata
{
    public delegate void SpellMetadataEventHandler(object sender, SpellMetadataEventArgs e);

    public sealed class SpellMetadataEventArgs : EventArgs
    {
        public SpellMetadata Spell { get; }

        public SpellMetadataEventArgs(SpellMetadata spell)
            => Spell = spell ?? throw new ArgumentNullException(nameof(spell));
    }
}
