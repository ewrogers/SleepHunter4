using System;
using System.Xml.Serialization;

using SleepHunter.Common;

namespace SleepHunter.Macro
{
    [Serializable]
    public sealed class SavedSkillState : ObservableObject
    {
        private string skillName;

        [XmlAttribute("Name")]
        public string SkillName
        {
            get => skillName;
            set => SetProperty(ref skillName, value);
        }

        public SavedSkillState(string skillName) => this.skillName = skillName;
    }
}
