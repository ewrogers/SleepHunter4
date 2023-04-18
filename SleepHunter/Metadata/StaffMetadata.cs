using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Metadata
{
    [Serializable]
    public sealed class StaffMetadata : ObservableObject
    {
        public static readonly StaffMetadata NoStaff = new StaffMetadata { Name = "No Staff", Class = PlayerClass.All };

        private string name;
        private int level;
        private int abilityLevel;
        private PlayerClass playerClass;
        private readonly List<SpellLineModifiers> lineModifiers = new List<SpellLineModifiers>();

        public event SpellLineModifiersEventHandler ModifiersAdded;
        public event SpellLineModifiersEventHandler ModifiersRemoved;

        [XmlAttribute(nameof(Name))]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        [XmlAttribute(nameof(Level))]
        [DefaultValue(0)]
        public int Level
        {
            get => level;
            set => SetProperty(ref level, value);
        }

        [XmlAttribute(nameof(AbilityLevel))]
        [DefaultValue(0)]
        public int AbilityLevel
        {
            get => abilityLevel;
            set => SetProperty(ref abilityLevel, value);
        }

        [XmlAttribute(nameof(Class))]
        public PlayerClass Class
        {
            get => playerClass;
            set => SetProperty(ref playerClass, value);
        }

        [XmlArray("LineModifiers")]
        [XmlArrayItem("Lines")]
        public List<SpellLineModifiers> Modifiers => lineModifiers;

        public void AddModifiers(SpellLineModifiers modifiers)
        {
            if (modifiers == null)
                throw new ArgumentNullException(nameof(modifiers));

            lineModifiers.Add(modifiers);
            OnModifiersAdded(modifiers);
        }

        public void AddAllModifiers(IEnumerable<SpellLineModifiers> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var modifiers in collection)
                AddModifiers(modifiers);
        }

        public bool RemoveModifiers(SpellLineModifiers modifiers)
        {
            if (modifiers == null)
                throw new ArgumentNullException(nameof(modifiers));

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
            for (var i = Modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = Modifiers[i];
                Modifiers.RemoveAt(i);

                OnModifiersRemoved(modifier);
            }
        }

        public void CopyTo(StaffMetadata other, bool copyModifiers = true)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            other.Name = Name;
            other.Level = Level;
            other.AbilityLevel = AbilityLevel;
            other.Class = Class;

            if (copyModifiers)
            {
                other.ClearModifiers();
                other.AddAllModifiers(Modifiers);
            }
        }

        private void OnModifiersAdded(SpellLineModifiers modifiers) => ModifiersAdded?.Invoke(this, new SpellLineModifiersEventArgs(modifiers));
        private void OnModifiersRemoved(SpellLineModifiers modifiers) => ModifiersRemoved?.Invoke(this, new SpellLineModifiersEventArgs(modifiers));

        public override string ToString() => Name ?? "Unknown Staff";

        public object Clone()
        {
            var copy = new StaffMetadata()
            {
                Name = Name,
                Level = Level,
                AbilityLevel = AbilityLevel,
                Class = Class
            };

            copy.AddAllModifiers(Modifiers);
            return copy;
        }
    }
}
