using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Macro
{
    [Serializable]
    internal sealed class SavedFlowerState : ObservableObject
    {
        private TargetCoordinateUnits targetMode;
        private string characterName;
        private double locationX;
        private double locationY;
        private double offsetX;
        private double offsetY;
        private int innerRadius;
        private int outerRadius;
        private bool hasInterval = true;
        private TimeSpan interval;
        private int manaThreshold;

        [XmlAttribute("Mode")]
        public TargetCoordinateUnits TargetMode
        {
            get { return targetMode; }
            set { SetProperty(ref targetMode, value); }
        }

        [XmlAttribute("TargetName")]
        [DefaultValue(null)]
        public string CharacterName
        {
            get { return characterName; }
            set { SetProperty(ref characterName, value); }
        }

        [XmlAttribute("X")]
        [DefaultValue(0)]
        public double LocationX
        {
            get { return locationX; }
            set { SetProperty(ref locationX, value); }
        }

        [XmlAttribute("Y")]
        [DefaultValue(0)]
        public double LocationY
        {
            get { return locationY; }
            set { SetProperty(ref locationY, value); }
        }

        [XmlAttribute("OffsetX")]
        [DefaultValue(0)]
        public double OffsetX
        {
            get { return offsetX; }
            set { SetProperty(ref offsetX, value); }
        }

        [XmlAttribute("OffsetY")]
        [DefaultValue(0)]
        public double OffsetY
        {
            get { return offsetY; }
            set { SetProperty(ref offsetY, value); }
        }

        [XmlAttribute("InnerRadius")]
        [DefaultValue(0)]
        public int InnerRadius
        {
            get { return innerRadius; }
            set { SetProperty(ref innerRadius, value); }
        }

        [XmlAttribute("OuterRadius")]
        [DefaultValue(0)]
        public int OuterRadius
        {
            get { return outerRadius; }
            set { SetProperty(ref outerRadius, value); }
        }

        [XmlIgnore]
        public TimeSpan Interval
        {
            get { return interval; }
            set { SetProperty(ref interval, value, onChanged: (p) => { RaisePropertyChanged("IntervalSeconds"); RaisePropertyChanged("HasInterval"); }); }
        }

        [XmlAttribute("HasInterval")]
        [DefaultValue(true)]
        public bool HasInterval
        {
            get { return hasInterval; }
            set { SetProperty(ref hasInterval, value); }
        }

        [XmlAttribute("Interval")]
        [DefaultValue(0.0)]
        public double IntervalSeconds
        {
            get { return interval.TotalSeconds; }
            set { Interval = TimeSpan.FromSeconds(value); }
        }

        [XmlAttribute("IfManaLessThan")]
        [DefaultValue(0)]
        public int ManaThreshold
        {
            get { return manaThreshold; }
            set { SetProperty(ref manaThreshold, value); }
        }

        public SavedFlowerState() { }

        public SavedFlowerState(SpellTarget target, TimeSpan? interval, int? manaThreshold = null)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            TargetMode = target.Units;
            CharacterName = target.CharacterName;
            LocationX = target.Location.X;
            LocationY = target.Location.Y;
            OffsetX = target.Offset.X;
            OffsetY = target.Offset.Y;
            InnerRadius = target.InnerRadius;
            OuterRadius = target.OuterRadius;

            if (interval.HasValue)
                Interval = interval.Value;

            if (manaThreshold.HasValue)
                ManaThreshold = manaThreshold.Value;
        }

        public SavedFlowerState(FlowerQueueItem item)
           : this(item.Target, item.Interval, item.ManaThreshold) { }
    }
}
