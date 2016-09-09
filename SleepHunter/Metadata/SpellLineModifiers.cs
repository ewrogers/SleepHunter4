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
         set { SetProperty(ref action, value, "Action"); }
      }

      [XmlAttribute("Scope")]
      public ModifierScope Scope
      {
         get { return scope; }
         set { SetProperty(ref scope, value, "Scope"); }
      }

      [XmlAttribute("Name")]
      [DefaultValue(null)]
      public string ScopeName
      {
         get { return scopeName; }
         set { SetProperty(ref scopeName, value, "ScopeName"); }
      }

      [XmlAttribute("Value")]
      public int Value
      {
         get { return value; }
         set { SetProperty(ref this.value, value, "Value"); }
      }

      [XmlAttribute("MinThreshold")]
      [DefaultValue(0)]
      public int MinThreshold
      {
         get { return minThreshold; }
         set { SetProperty(ref minThreshold, value, "MinThreshold"); }
      }

      [XmlAttribute("MaxThreshold")]
      [DefaultValue(0)]
      public int MaxThreshold
      {
         get { return maxThreshold; }
         set { SetProperty(ref maxThreshold, value, "MaxThreshold"); }
      }

      public SpellLineModifiers()
      {

      }

      public override string ToString()
      {
         var sb = new StringBuilder(256);

         sb.Append(this.Action.ToString());

         switch (this.Scope)
         {
            case ModifierScope.All:
               sb.Append(" all");
               break;

            case ModifierScope.Group:
               sb.AppendFormat(" all {0}", this.ScopeName);
               break;

            case ModifierScope.Single:
               sb.AppendFormat(" {0}", this.ScopeName);
               break;
         }

         if (this.Scope != ModifierScope.Single)
         {
            if (this.MinThreshold <= 0 && this.MaxThreshold <= 0)
               sb.Append(" spells");
            else if (this.MinThreshold == this.MaxThreshold && (this.MinThreshold > 0))
               sb.AppendFormat(" {0} line spells", this.MaxThreshold.ToString());
            else if (this.MinThreshold > 0 && this.MaxThreshold > 0)
               sb.AppendFormat(" spells between {0} and {1} lines", this.MinThreshold.ToString(), this.MaxThreshold.ToString());
            else if (this.MinThreshold > 0)
               sb.AppendFormat(" spells {0} and over", this.MinThreshold.ToPluralString(" lines", " line"));
            else if (this.MaxThreshold > 0)
               sb.AppendFormat(" spells {0} and under", this.MaxThreshold.ToPluralString(" lines", " line"));
         }
         
         if (this.Action == ModifierAction.Set)
            sb.AppendFormat(" to {0}", this.Value.ToPluralString(" lines", " line"));
         else
            sb.AppendFormat(" by {0}", this.Value.ToPluralString(" lines", " line"));

         if (this.Scope == ModifierScope.Single)
         {
            if (this.MinThreshold == this.MaxThreshold && (this.MinThreshold > 0))
               sb.AppendFormat(" if exactly {0}", this.MaxThreshold.ToPluralString(" lines", " line"));
            else if (this.MinThreshold > 0 && this.MaxThreshold > 0)
               sb.AppendFormat(" when between {0} and {1} lines", this.MinThreshold.ToString(), this.MaxThreshold.ToString());
            else if (this.MinThreshold > 0)
               sb.AppendFormat(" when {0} and over", this.MinThreshold.ToPluralString(" lines", " line"));
            else if (this.MaxThreshold > 0)
               sb.AppendFormat(" when {0} and under", this.MaxThreshold.ToPluralString(" lines", " line"));
         }

         sb.Append(".");
         return sb.ToString();
      }

      public void CopyTo(SpellLineModifiers other)
      {
         other.Action = this.Action;
         other.Scope = this.Scope;
         other.ScopeName = this.ScopeName;
         other.Value = this.Value;
         other.MinThreshold = this.MinThreshold;
         other.MaxThreshold = this.MaxThreshold;
      }
   }
}
