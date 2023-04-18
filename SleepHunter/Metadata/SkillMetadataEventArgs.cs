using System;

namespace SleepHunter.Metadata
{
    internal delegate void SkillMetadataEventHandler(object sender, SkillMetadataEventArgs e);

    internal sealed class SkillMetadataEventArgs : EventArgs
    {
        readonly SkillMetadata skill;

        public SkillMetadata Skill
        {
            get { return skill; }
        }

        public SkillMetadataEventArgs(SkillMetadata skill)
        {
            this.skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }
    }
}
