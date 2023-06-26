using System;
using System.ComponentModel;
using SleepHunter.Models;
using System.Xml.Serialization;

namespace SleepHunter.Services.Serialization
{
    [Serializable]
    public sealed class SerializedFlowerState
    {
        [XmlAttribute("Mode")]
        public SpellTargetMode TargetMode { get; set; }

        [XmlAttribute("Target")]
        [DefaultValue(null)]
        public string CharacterName { get; set; }

        [XmlAttribute("X")]
        [DefaultValue(0)]
        public double LocationX { get; set; }

        [XmlAttribute("Y")]
        [DefaultValue(0)]
        public double LocationY { get; set; }

        [XmlAttribute("OffsetX")]
        [DefaultValue(0)]
        public double OffsetX { get; set; }

        [XmlAttribute("OffsetY")]
        [DefaultValue(0)]
        public double OffsetY { get; set; }

        [XmlAttribute("InnerRadius")]
        [DefaultValue(0)]
        public int InnerRadius { get; set; }

        [XmlAttribute("OuterRadius")]
        [DefaultValue(0)]
        public int OuterRadius { get; set; }

        [XmlIgnore]
        public TimeSpan Interval { get; set; }

        [XmlAttribute("HasInterval")]
        [DefaultValue(true)]
        public bool HasInterval { get; set; }

        [XmlAttribute("Interval")]
        [DefaultValue(0.0)]
        public double IntervalSeconds
        {
            get => Interval.TotalSeconds;
            set => Interval = TimeSpan.FromSeconds(value);
        }

        [XmlAttribute("IfManaLessThan")]
        [DefaultValue(0)]
        public int ManaThreshold { get; set; }
    }
}
