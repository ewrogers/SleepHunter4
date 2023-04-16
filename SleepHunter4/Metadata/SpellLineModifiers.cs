using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Extensions;

namespace SleepHunter.Metadata
{
    public enum ModifierAction
    {
        None = 0,
        Increase,
        Decrease,
        Set
    }

    public enum ModifierScope
    {
        None = 0,
        Single,
        Group,
        All
    }

    [Serializable]
    public sealed class SpellLineModifiers : ObservableObject
    {
        ModifierAction action;
        ModifierScope scope;
        string scopeName;
        int value;
        int minThreshold;
        int maxThreshold;

        [XmlAttribute("Action")]
        public ModifierAction Action
        {
            get { return action; }
            set { SetProperty(ref action, value); }
        }

        [XmlAttribute("Scope")]
        public ModifierScope Scope
        {
            get { return scope; }
            set { SetProperty(ref scope, value); }
        }

        [XmlAttribute("Name")]
        [DefaultValue(null)]
        public string ScopeName
        {
            get { return scopeName; }
            set { SetProperty(ref scopeName, value); }
        }

        [XmlAttribute("Value")]
        public int Value
        {
            get { return value; }
            set { SetProperty(ref this.value, value); }
        }

        [XmlAttribute("MinThreshold")]
        [DefaultValue(0)]
        public int MinThreshold
        {
            get { return minThreshold; }
            set { SetProperty(ref minThreshold, value); }
        }

        [XmlAttribute("MaxThreshold")]
        [DefaultValue(0)]
        public int MaxThreshold
        {
            get { return maxThreshold; }
            set { SetProperty(ref maxThreshold, value); }
        }

        public SpellLineModifiers() { }

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

        public void CopyTo(SpellLineModifiers other)
        {
            other.Action = Action;
            other.Scope = Scope;
            other.ScopeName = ScopeName;
            other.Value = Value;
            other.MinThreshold = MinThreshold;
            other.MaxThreshold = MaxThreshold;
        }
    }
}
