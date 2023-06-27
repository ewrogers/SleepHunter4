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
        private string name;
        private PlayerClass playerClass;
        private string groupName;
        private int manaCost;
        private bool isAssail;
        private bool opensDialog;
        private bool canImprove = true;
        private TimeSpan cooldown;
        private bool requiresDisarm;
        private double minHealthPercent;
        private double maxHealthPercent;

        [XmlAttribute("Name")]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        [XmlAttribute("Class")]
        [DefaultValue(PlayerClass.Peasant)]
        public PlayerClass Class
        {
            get => playerClass;
            set => SetProperty(ref playerClass, value);
        }

        [XmlAttribute("Group")]
        [DefaultValue(null)]
        public string GroupName
        {
            get => groupName;
            set => SetProperty(ref groupName, value);
        }

        [XmlAttribute("Mana")]
        [DefaultValue(0)]
        public int ManaCost
        {
            get => manaCost;
            set => SetProperty(ref manaCost, value);
        }

        [XmlAttribute("IsAssail")]
        [DefaultValue(false)]
        public bool IsAssail
        {
            get => isAssail;
            set => SetProperty(ref isAssail, value);
        }

        [XmlAttribute("OpensDialog")]
        [DefaultValue(false)]
        public bool OpensDialog
        {
            get => opensDialog;
            set => SetProperty(ref opensDialog, value);
        }

        [XmlAttribute("CanImprove")]
        [DefaultValue(true)]
        public bool CanImprove
        {
            get => canImprove;
            set => SetProperty(ref canImprove, value);
        }

        [XmlIgnore]
        public TimeSpan Cooldown
        {
            get => cooldown;
            set => SetProperty(ref cooldown, value, onChanged: (s) => { RaisePropertyChanged(nameof(CooldownSeconds)); });
        }

        [XmlAttribute("Cooldown")]
        [DefaultValue(0.0)]
        public double CooldownSeconds
        {
            get => cooldown.TotalSeconds;
            set => Cooldown = TimeSpan.FromSeconds(value);
        }

        [XmlAttribute("RequiresDisarm")]
        [DefaultValue(false)]
        public bool RequiresDisarm
        {
            get => requiresDisarm;
            set => SetProperty(ref requiresDisarm, value);
        }

        [XmlAttribute("MinHealthPercent")]
        [DefaultValue(null)]
        public double MinHealthPercent
        {
            get => minHealthPercent;
            set => SetProperty(ref  minHealthPercent, value);
        }

        [XmlAttribute("MaxHealthPercent")]
        [DefaultValue(null)]
        public double MaxHealthPercent
        {
            get => maxHealthPercent;
            set => SetProperty(ref maxHealthPercent, value);
        }

        public SkillMetadata() { }

        public override string ToString() => Name ?? "Unknown Skill";

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
            other.MinHealthPercent = MinHealthPercent;
            other.MaxHealthPercent = MaxHealthPercent;
        }
    }
}
