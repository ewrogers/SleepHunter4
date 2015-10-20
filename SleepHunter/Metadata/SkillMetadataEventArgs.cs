using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Metadata
{
   public delegate void SkillMetadataEventHandler(object sender, SkillMetadataEventArgs e);

   public sealed class SkillMetadataEventArgs : EventArgs
   {
      readonly SkillMetadata skill;

      public SkillMetadata Skill
      {
         get { return skill; }
      }

      public SkillMetadataEventArgs(SkillMetadata skill)
      {
         if (skill == null)
            throw new ArgumentNullException("skill");

         this.skill = skill;
      }
   }
}
