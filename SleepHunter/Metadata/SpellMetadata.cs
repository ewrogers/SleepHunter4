using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Metadata
{
    [Serializable]
    public sealed class SpellMetadata : ObservableObject
    {
        private string name;
        private PlayerClass playerClass;
        private string groupName;
        private int manaCost;
        private bool opensDialog;
        private int numberOfLines;
        private TimeSpan cooldown;
        private bool canImprove = true;
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

        [XmlAttribute("OpensDialog")]
        [DefaultValue(false)]
        public bool OpensDialog
        {
            get => opensDialog;
            set => SetProperty(ref opensDialog, value);
        }

        [XmlAttribute("Lines")]
        [DefaultValue(0)]
        public int NumberOfLines
        {
            get => numberOfLines;
            set => SetProperty(ref numberOfLines, value);
        }

        [XmlIgnore]
        public TimeSpan Cooldown
        {
            get => cooldown;
            set => SetProperty(ref cooldown, value);
        }

        [XmlAttribute("Cooldown")]
        [DefaultValue(0.0)]
        public double CooldownSeconds
        {
            get => cooldown.TotalSeconds;
            set => Cooldown = TimeSpan.FromSeconds(value);
        }

        [XmlAttribute("CanImprove")]
        [DefaultValue(true)]
        public bool CanImprove
        {
            get => canImprove;
            set => SetProperty(ref canImprove, value);
        }

        [XmlAttribute("MinHealthPercent")]
        [DefaultValue(0)]
        public double MinHealthPercent
        {
            get => minHealthPercent;
            set => SetProperty(ref minHealthPercent, value);
        }

        [XmlAttribute("MaxHealthPercent")]
        [DefaultValue(0)]
        public double MaxHealthPercent
        {
            get => maxHealthPercent;
            set => SetProperty(ref maxHealthPercent, value);
        }

        public SpellMetadata() { }

        public override string ToString() => Name ?? "Unknown Spell";

        public void CopyTo(SpellMetadata other)
        {
            other.Name = Name;
            other.Class = Class;
            other.GroupName = GroupName;
            other.ManaCost = ManaCost;
            other.OpensDialog = OpensDialog;
            other.NumberOfLines = NumberOfLines;
            other.Cooldown = Cooldown;
            other.CanImprove = CanImprove;
            other.MinHealthPercent = MinHealthPercent;
            other.MaxHealthPercent = MaxHealthPercent;
        }
    }
}
