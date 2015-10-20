using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using SleepHunter.Data;

namespace SleepHunter.Macro
{
   [Serializable]
   public sealed class SavedSpellState : NotifyObject
   {
      string spellName;
      TargetCoordinateUnits targetMode;
      string characterName;
      double locationX;
      double locationY;
      double offsetX;
      double offsetY;
      int innerRadius;
      int outerRadius;
      int targetLevel;

      [XmlAttribute("Name")]
      public string SpellName
      {
         get { return spellName; }
         set { SetProperty(ref spellName, value, "SpellName"); }
      }

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

      [XmlAttribute("TargetLevel")]
      [DefaultValue(0)]
      public int TargetLevel
      {
         get { return targetLevel; }
         set { SetProperty(ref targetLevel, value, "TargetLevel"); }
      }

      public SavedSpellState()
      {

      }

      public SavedSpellState(string spellName, SpellTarget target, int? targetLevel = null)
      {
         if (target == null)
            throw new ArgumentNullException("target");

         this.SpellName = spellName;
         this.TargetMode = target.Units;
         this.CharacterName = target.CharacterName;
         this.LocationX = target.Location.X;
         this.LocationY = target.Location.Y;
         this.OffsetX = target.Offset.X;
         this.OffsetY = target.Offset.Y;
         this.InnerRadius = target.InnerRadius;
         this.OuterRadius = target.OuterRadius;

         if (targetLevel.HasValue)
            this.TargetLevel = targetLevel.Value;
      }

      public SavedSpellState(SpellQueueItem item)
         : this(item.Name, item.Target, item.TargetLevel) { }
   }
}
