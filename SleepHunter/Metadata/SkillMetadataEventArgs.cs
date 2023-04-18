using System;

namespace SleepHunter.Metadata
{
    public delegate void SkillMetadataEventHandler(object sender, SkillMetadataEventArgs e);

    public sealed class SkillMetadataEventArgs : EventArgs
    {
        public SkillMetadata Skill { get; }

        public SkillMetadataEventArgs(SkillMetadata skill)
            => Skill = skill ?? throw new ArgumentNullException(nameof(skill));
    }
}
