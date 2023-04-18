using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Extensions;

namespace SleepHunter.Metadata
{
    [Serializable]
    public sealed class SpellLineModifiers : ObservableObject, ICloneable
    {
        private ModifierAction action;
        private ModifierScope scope;
        private string scopeName;
        private int value;
        private int minThreshold;
        private int maxThreshold;

        [XmlAttribute(nameof(Action))]
        public ModifierAction Action
        {
            get => action;
            set => SetProperty(ref action, value);
        }

        [XmlAttribute(nameof(Scope))]
        public ModifierScope Scope
        {
            get => scope;
            set => SetProperty(ref scope, value);
        }

        [XmlAttribute("Name")]
        [DefaultValue(null)]
        public string ScopeName
        {
            get => scopeName;
            set => SetProperty(ref scopeName, value);
        }

        [XmlAttribute(nameof(Value))]
        public int Value
        {
            get => value;
            set => SetProperty(ref this.value, value);
        }

        [XmlAttribute(nameof(MinThreshold))]
        [DefaultValue(0)]
        public int MinThreshold
        {
            get => minThreshold;
            set => SetProperty(ref minThreshold, value);
        }

        [XmlAttribute(nameof(MaxThreshold))]
        [DefaultValue(0)]
        public int MaxThreshold
        {
            get => maxThreshold;
            set => SetProperty(ref maxThreshold, value);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(256);

            sb.Append(Action.ToString());

            switch (Scope)
            {
                case ModifierScope.All:
                    sb.Append(" all");
                    break;

                case ModifierScope.Group:
                    sb.AppendFormat(" all {0}", ScopeName);
                    break;

                case ModifierScope.Single:
                    sb.AppendFormat(" {0}", ScopeName);
                    break;
            }

            if (Scope != ModifierScope.Single)
            {
                if (MinThreshold <= 0 && MaxThreshold <= 0)
                    sb.Append(" spells");
                else if (MinThreshold == MaxThreshold && (MinThreshold > 0))
                    sb.AppendFormat(" {0} line spells", MaxThreshold.ToString());
                else if (MinThreshold > 0 && MaxThreshold > 0)
                    sb.AppendFormat(" spells between {0} and {1} lines", MinThreshold.ToString(), MaxThreshold.ToString());
                else if (MinThreshold > 0)
                    sb.AppendFormat(" spells {0} and over", MinThreshold.ToPluralString(" lines", " line"));
                else if (MaxThreshold > 0)
                    sb.AppendFormat(" spells {0} and under", MaxThreshold.ToPluralString(" lines", " line"));
            }

            if (Action == ModifierAction.Set)
                sb.AppendFormat(" to {0}", Value.ToPluralString(" lines", " line"));
            else
                sb.AppendFormat(" by {0}", Value.ToPluralString(" lines", " line"));

            if (Scope == ModifierScope.Single)
            {
                if (MinThreshold == MaxThreshold && (MinThreshold > 0))
                    sb.AppendFormat(" if exactly {0}", MaxThreshold.ToPluralString(" lines", " line"));
                else if (MinThreshold > 0 && MaxThreshold > 0)
                    sb.AppendFormat(" when between {0} and {1} lines", MinThreshold.ToString(), MaxThreshold.ToString());
                else if (MinThreshold > 0)
                    sb.AppendFormat(" when {0} and over", MinThreshold.ToPluralString(" lines", " line"));
                else if (MaxThreshold > 0)
                    sb.AppendFormat(" when {0} and under", MaxThreshold.ToPluralString(" lines", " line"));
            }

            sb.Append(".");
            return sb.ToString();
        }

        public object Clone()
        {
            return new SpellLineModifiers()
            {
                Action = Action,
                Scope = Scope,
                ScopeName = ScopeName,
                Value = Value,
                MinThreshold = MinThreshold,
                MaxThreshold = MaxThreshold
            };
        }
    }
}
