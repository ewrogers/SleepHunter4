using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Macro
{
    [Serializable]
    public sealed class SavedSpellState : ObservableObject
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
            set { SetProperty(ref spellName, value); }
        }

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

        [XmlAttribute("TargetLevel")]
        [DefaultValue(0)]
        public int TargetLevel
        {
            get { return targetLevel; }
            set { SetProperty(ref targetLevel, value); }
        }

        public SavedSpellState() { }

        public SavedSpellState(string spellName, SpellTarget target, int? targetLevel = null)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            SpellName = spellName;
            TargetMode = target.Units;
            CharacterName = target.CharacterName;
            LocationX = target.Location.X;
            LocationY = target.Location.Y;
            OffsetX = target.Offset.X;
            OffsetY = target.Offset.Y;
            InnerRadius = target.InnerRadius;
            OuterRadius = target.OuterRadius;

            if (targetLevel.HasValue)
                TargetLevel = targetLevel.Value;
        }

        public SavedSpellState(SpellQueueItem item)
           : this(item.Name, item.Target, item.TargetLevel) { }
    }
}
