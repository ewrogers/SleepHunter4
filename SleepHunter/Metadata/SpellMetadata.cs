using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Metadata
{
    [Serializable]
    public sealed class SpellMetadata : ObservableObject, ICloneable
    {
        private string name;
        private PlayerClass playerClass;
        private string groupName;
        private int manaCost;
        private int numberOfLines;
        private TimeSpan cooldown;
        private bool canImprove = true;

        [XmlAttribute(nameof(Name))]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        [XmlAttribute(nameof(Class))]
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

        [XmlAttribute(nameof(CanImprove))]
        [DefaultValue(true)]
        public bool CanImprove
        {
            get => canImprove;
            set => SetProperty(ref canImprove, value);
        }

        public override string ToString() => Name ?? "Unknown Spell";

        public object Clone()
        {
            return new SpellMetadata
            {
                Name = Name,
                Class = Class,
                GroupName = GroupName,
                ManaCost = ManaCost,
                NumberOfLines = NumberOfLines,
                Cooldown = Cooldown,
                CanImprove = CanImprove
            };
        }
    }
}
