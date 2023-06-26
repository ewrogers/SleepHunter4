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
        private string spellName;
        private SpellTargetMode targetMode;
        private string characterName;
        private double locationX;
        private double locationY;
        private double offsetX;
        private double offsetY;
        private int innerRadius;
        private int outerRadius;
        private int targetLevel;

        [XmlAttribute("Name")]
        public string SpellName
        {
            get => spellName;
            set => SetProperty(ref spellName, value);
        }

        [XmlAttribute("Mode")]
        public SpellTargetMode TargetMode
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

        [XmlAttribute("OffsetX")]
        [DefaultValue(0)]
        public double OffsetX
        {
            get => offsetX;
            set => SetProperty(ref offsetX, value);
        }

        [XmlAttribute("OffsetY")]
        [DefaultValue(0)]
        public double OffsetY
        {
            get => offsetY;
            set => SetProperty(ref offsetY, value);
        }

        [XmlAttribute("InnerRadius")]
        [DefaultValue(0)]
        public int InnerRadius
        {
            get => innerRadius;
            set => SetProperty(ref innerRadius, value);
        }

        [XmlAttribute("OuterRadius")]
        [DefaultValue(0)]
        public int OuterRadius
        {
            get => outerRadius;
            set => SetProperty(ref outerRadius, value);
        }

        [XmlAttribute("TargetLevel")]
        [DefaultValue(0)]
        public int TargetLevel
        {
            get => targetLevel;
            set => SetProperty(ref targetLevel, value);
        }

        public SavedSpellState() { }

        public SavedSpellState(string spellName, SpellTarget target, int? targetLevel = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

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
