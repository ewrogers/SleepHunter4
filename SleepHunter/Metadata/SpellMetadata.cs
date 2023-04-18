using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Metadata
{
    [Serializable]
    internal sealed class SpellMetadata : ObservableObject
    {
        private string name;
        private PlayerClass playerClass;
        private string groupName;
        private int manaCost;
        private int numberOfLines;
        private TimeSpan cooldown;
        private bool canImprove = true;

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

        [XmlAttribute("Lines")]
        [DefaultValue(0)]
        public int NumberOfLines
        {
            get { return numberOfLines; }
            set { SetProperty(ref numberOfLines, value); }
        }

        [XmlIgnore]
        public TimeSpan Cooldown
        {
            get { return cooldown; }
            set { SetProperty(ref cooldown, value); }
        }

        [XmlAttribute("Cooldown")]
        [DefaultValue(0.0)]
        public double CooldownSeconds
        {
            get { return cooldown.TotalSeconds; }
            set { Cooldown = TimeSpan.FromSeconds(value); }
        }

        [XmlAttribute("CanImprove")]
        [DefaultValue(true)]
        public bool CanImprove
        {
            get { return canImprove; }
            set { SetProperty(ref canImprove, value); }
        }

        public SpellMetadata() { }

        public override string ToString()
        {
            return Name ?? "Unknown Spell";
        }

        public void CopyTo(SpellMetadata other)
        {
            other.Name = Name;
            other.Class = Class;
            other.GroupName = GroupName;
            other.ManaCost = ManaCost;
            other.NumberOfLines = NumberOfLines;
            other.Cooldown = Cooldown;
            other.CanImprove = CanImprove;
        }
    }
}
