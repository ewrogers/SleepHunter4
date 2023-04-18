using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Metadata
{
    [Serializable]
    internal sealed class StaffMetadata : ObservableObject
    {
        public static readonly StaffMetadata NoStaff = new StaffMetadata { Name = "No Staff", Class = PlayerClass.All };

        private string name;
        private int level;
        private int abilityLevel;
        private PlayerClass playerClass;
        private List<SpellLineModifiers> lineModifiers = new List<SpellLineModifiers>();

        public event SpellLineModifiersEventHandler ModifiersAdded;
        public event SpellLineModifiersEventHandler ModifiersChanged;
        public event SpellLineModifiersEventHandler ModifiersRemoved;

        [XmlAttribute("Name")]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        [XmlAttribute("Level")]
        [DefaultValue(0)]
        public int Level
        {
            get { return level; }
            set { SetProperty(ref level, value); }
        }

        [XmlAttribute("AbilityLevel")]
        [DefaultValue(0)]
        public int AbilityLevel
        {
            get { return abilityLevel; }
            set { SetProperty(ref abilityLevel, value); }
        }

        [XmlAttribute("Class")]
        public PlayerClass Class
        {
            get { return playerClass; }
            set { SetProperty(ref playerClass, value); }
        }

        [XmlArray("LineModifiers")]
        [XmlArrayItem("Lines")]
        public List<SpellLineModifiers> Modifiers
        {
            get { return lineModifiers; }
            private set { SetProperty(ref lineModifiers, value); }
        }

        public void AddModifiers(SpellLineModifiers modifiers)
        {
            if (modifiers == null)
                throw new ArgumentNullException("modifiers");

            lineModifiers.Add(modifiers);
            OnModifiersAdded(modifiers);
        }

        public void ChangeModifiers(SpellLineModifiers target, SpellLineModifiers source)
        {
            source.CopyTo(target);
            OnModifiersChanged(target);
        }

        public bool RemoveModifiers(SpellLineModifiers modifiers)
        {
            if (modifiers == null)
                throw new ArgumentNullException("modifiers");

            SpellLineModifiers removedModifiers = null;

            foreach (var modifier in lineModifiers)
                if (modifier == modifiers)
                {
                    removedModifiers = modifier;
                    break;
                }

            var wasRemoved = lineModifiers.Remove(modifiers);

            if (removedModifiers != null && wasRemoved)
                OnModifiersRemoved(removedModifiers);

            return wasRemoved;
        }

        public void ClearModifiers()
        {
            foreach (var modifier in lineModifiers)
                OnModifiersRemoved(modifier);

            lineModifiers.Clear();
        }

        private void OnModifiersAdded(SpellLineModifiers modifiers) => ModifiersAdded?.Invoke(this, new SpellLineModifiersEventArgs(modifiers));

        private void OnModifiersChanged(SpellLineModifiers modifiers) => ModifiersChanged?.Invoke(this, new SpellLineModifiersEventArgs(modifiers));

        private void OnModifiersRemoved(SpellLineModifiers modifiers) => ModifiersRemoved?.Invoke(this, new SpellLineModifiersEventArgs(modifiers));

        public override string ToString() => Name ?? "Unknown Staff";

        public void CopyTo(StaffMetadata other, bool copyModifiers = true)
        {
            other.Name = Name;
            other.Level = Level;
            other.AbilityLevel = AbilityLevel;
            other.Class = Class;

            if (copyModifiers)
                other.Modifiers = new List<SpellLineModifiers>(Modifiers);
        }
    }
}
