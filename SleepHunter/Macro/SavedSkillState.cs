using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using SleepHunter.Data;

namespace SleepHunter.Macro
{
   [Serializable]
   public sealed class SavedSkillState : NotifyObject
   {
      string skillName;

      [XmlAttribute("Name")]
      public string SkillName
      {
         get { return skillName; }
         set { SetProperty(ref skillName, value, "SkillName"); }
      }

      public SavedSkillState()
      {

      }

      public SavedSkillState(string skillName)
      {
         this.skillName = skillName;
      }
   }
}
