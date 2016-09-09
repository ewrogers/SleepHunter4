using System;
using System.Xml.Serialization;

using SleepHunter.Common;

namespace SleepHunter.Macro
{
    [Serializable]
   public sealed class SavedSkillState : ObservableObject
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
