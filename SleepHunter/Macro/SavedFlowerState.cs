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
      TargetCoordinateUnits targetMode;
      string characterName;
      double locationX;
      double locationY;
      double offsetX;
      double offsetY;
      int innerRadius;
      int outerRadius;
      bool hasInterval = true;
      TimeSpan interval;
      int manaThreshold;

      [XmlAttribute("Mode")]
      public TargetCoordinateUnits TargetMode
      {
         get { return targetMode; }
         set { SetProperty(ref targetMode, value, "TargetMode"); }
      }

      [XmlAttribute("TargetName")]
      [DefaultValue(null)]
      public string CharacterName
      {
         get { return characterName; }
         set { SetProperty(ref characterName, value, "CharacterName"); }
      }

      [XmlAttribute("X")]
      [DefaultValue(0)]
      public double LocationX
      {
         get { return locationX; }
         set { SetProperty(ref locationX, value, "LocationX"); }
      }

      [XmlAttribute("Y")]
      [DefaultValue(0)]
      public double LocationY
      {
         get { return locationY; }
         set { SetProperty(ref locationY, value, "LocationY"); }
      }

      [XmlAttribute("OffsetX")]
      [DefaultValue(0)]
      public double OffsetX
      {
         get { return offsetX; }
         set { SetProperty(ref offsetX, value, "OffsetX"); }
      }

      [XmlAttribute("OffsetY")]
      [DefaultValue(0)]
      public double OffsetY
      {
         get { return offsetY; }
         set { SetProperty(ref offsetY, value, "OffsetY"); }
      }

      [XmlAttribute("InnerRadius")]
      [DefaultValue(0)]
      public int InnerRadius
      {
         get { return innerRadius; }
         set { SetProperty(ref innerRadius, value, "InnerRadius"); }
      }

      [XmlAttribute("OuterRadius")]
      [DefaultValue(0)]
      public int OuterRadius
      {
         get { return outerRadius; }
         set { SetProperty(ref outerRadius, value, "OuterRadius"); }
      }

      [XmlIgnore]
      public TimeSpan Interval
      {
         get { return interval; }
         set { SetProperty(ref interval, value, "Interval", onChanged: (p) => { OnPropertyChanged("IntervalSeconds"); OnPropertyChanged("HasInterval"); }); }
      }

      [XmlAttribute("HasInterval")]
      [DefaultValue(true)]
      public bool HasInterval
      {
         get { return hasInterval; }
         set { SetProperty(ref hasInterval, value, "HasInterval"); }
      }

      [XmlAttribute("Interval")]
      [DefaultValue(0.0)]
      public double IntervalSeconds
      {
         get { return interval.TotalSeconds; }
         set { this.Interval = TimeSpan.FromSeconds(value); }
      }

      [XmlAttribute("IfManaLessThan")]
      [DefaultValue(0)]
      public int ManaThreshold
      {
         get { return manaThreshold; }
         set { SetProperty(ref manaThreshold, value, "ManaThreshold"); }
      }

      public SavedFlowerState()
      {

      }

      public SavedFlowerState(SpellTarget target, TimeSpan? interval, int? manaThreshold = null)
      {
         if (target == null)
            throw new ArgumentNullException("target");

         this.TargetMode = target.Units;
         this.CharacterName = target.CharacterName;
         this.LocationX = target.Location.X;
         this.LocationY = target.Location.Y;
         this.OffsetX = target.Offset.X;
         this.OffsetY = target.Offset.Y;
         this.InnerRadius = target.InnerRadius;
         this.OuterRadius = target.OuterRadius;

         if (interval.HasValue)
            this.Interval = interval.Value;

         if (manaThreshold.HasValue)
            this.ManaThreshold = manaThreshold.Value;
      }

      public SavedFlowerState(FlowerQueueItem item)
         : this(item.Target, item.Interval, item.ManaThreshold) { }
   }
}
