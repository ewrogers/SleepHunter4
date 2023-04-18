using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Macro
{
    [Serializable]
    public sealed class SavedFlowerState : ObservableObject
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
            get => targetMode;
            set => SetProperty(ref targetMode, value);
        }

        [XmlAttribute("TargetName")]
        [DefaultValue(null)]
        public string CharacterName
        {
            get => characterName;
            set => SetProperty(ref characterName, value);
        }

        [XmlAttribute("X")]
        [DefaultValue(0)]
        public double LocationX
        {
            get => locationX;
            set => SetProperty(ref locationX, value);
        }

        [XmlAttribute("Y")]
        [DefaultValue(0)]
        public double LocationY
        {
            get => locationY;
            set => SetProperty(ref locationY, value);
        }

        [XmlAttribute(nameof(OffsetX))]
        [DefaultValue(0)]
        public double OffsetX
        {
            get => offsetX;
            set => SetProperty(ref offsetX, value);
        }

        [XmlAttribute(nameof(OffsetY))]
        [DefaultValue(0)]
        public double OffsetY
        {
            get => offsetY;
            set => SetProperty(ref offsetY, value);
        }

        [XmlAttribute(nameof(InnerRadius))]
        [DefaultValue(0)]
        public int InnerRadius
        {
            get => innerRadius;
            set => SetProperty(ref innerRadius, value);
        }

        [XmlAttribute(nameof(OuterRadius))]
        [DefaultValue(0)]
        public int OuterRadius
        {
            get => outerRadius;
            set => SetProperty(ref outerRadius, value);
        }

        [XmlIgnore]
        public TimeSpan Interval
        {
            get => interval;
            set => SetProperty(ref interval, value, onChanged: (p) =>
            {
                RaisePropertyChanged(nameof(IntervalSeconds));
                RaisePropertyChanged(nameof(HasInterval));
            });
        }

        [XmlAttribute(nameof(HasInterval))]
        [DefaultValue(true)]
        public bool HasInterval
        {
            get => hasInterval;
            set => SetProperty(ref hasInterval, value);
        }

        [XmlAttribute("Interval")]
        [DefaultValue(0.0)]
        public double IntervalSeconds
        {
            get => interval.TotalSeconds;
            set => Interval = TimeSpan.FromSeconds(value);
        }

        [XmlAttribute("IfManaLessThan")]
        [DefaultValue(0)]
        public int ManaThreshold
        {
            get => manaThreshold;
            set => SetProperty(ref manaThreshold, value);
        }

        public SavedFlowerState(SpellTarget target, TimeSpan? interval, int? manaThreshold = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

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
