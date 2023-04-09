using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Metadata
{
    [Serializable]
    public sealed class SkillMetadata : ObservableObject
    {
        string name;
        PlayerClass playerClass;
        string groupName;
        int manaCost;
        bool isAssail;
        bool opensDialog;
        bool canImprove = true;
        TimeSpan cooldown;
        bool requiresDisarm;

        [XmlAttribute("Name")]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        [XmlAttribute("Class")]
        [DefaultValue(PlayerClass.Peasant)]
        public PlayerClass Class
        {
            get { return playerClass; }
            set { SetProperty(ref playerClass, value); }
        }

        [XmlAttribute("Group")]
        [DefaultValue(null)]
        public string GroupName
        {
            get { return groupName; }
            set { SetProperty(ref groupName, value); }
        }

        [XmlAttribute("Mana")]
        [DefaultValue(0)]
        public int ManaCost
        {
            get { return manaCost; }
            set { SetProperty(ref manaCost, value); }
        }

        [XmlAttribute("IsAssail")]
        [DefaultValue(false)]
        public bool IsAssail
        {
            get { return isAssail; }
            set { SetProperty(ref isAssail, value); }
        }

        [XmlAttribute("OpensDialog")]
        [DefaultValue(false)]
        public bool OpensDialog
        {
            get { return opensDialog; }
            set { SetProperty(ref opensDialog, value); }
        }

        [XmlAttribute("CanImprove")]
        [DefaultValue(true)]
        public bool CanImprove
        {
            get { return canImprove; }
            set { SetProperty(ref canImprove, value); }
        }

        [XmlIgnore]
        public TimeSpan Cooldown
        {
            get { return cooldown; }
            set { SetProperty(ref cooldown, value, onChanged: (s) => { RaisePropertyChanged("CooldownSeconds"); }); }
        }

        [XmlAttribute("Cooldown")]
        [DefaultValue(0.0)]
        public double CooldownSeconds
        {
            get { return cooldown.TotalSeconds; }
            set { Cooldown = TimeSpan.FromSeconds(value); }
        }

        [XmlAttribute("RequiresDisarm")]
        [DefaultValueAttribute(false)]
        public bool RequiresDisarm
        {
            get { return requiresDisarm; }
            set { SetProperty(ref requiresDisarm, value); }
        }

        public SkillMetadata() { }

        public override string ToString()
        {
            return Name ?? "Unknown Skill";
        }

        public void CopyTo(SkillMetadata other)
        {
            other.Name = Name;
            other.Class = Class;
            other.GroupName = GroupName;
            other.ManaCost = ManaCost;
            other.Cooldown = Cooldown;
            other.IsAssail = IsAssail;
            other.OpensDialog = OpensDialog;
            other.CanImprove = CanImprove;
            other.RequiresDisarm = RequiresDisarm;
        }
    }
}
