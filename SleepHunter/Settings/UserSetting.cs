using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using SleepHunter.Data;

namespace SleepHunter.Settings
{
   [Serializable]
   public sealed class UserSetting : NotifyObject
   {
      string key;
      string displayText;
      object value;

      [XmlAttribute("Key")]
      public string Key
      {
         get { return key; }
         set { SetProperty(ref key, value, "Key"); }
      }

      [XmlIgnore]
      public string DisplayText
      {
         get { return displayText; }
         set { SetProperty(ref displayText, value, "DisplayText"); }
      }

      [XmlAttribute("Value")]
      [DefaultValue(null)]
      public object Value
      {
         get { return value; }
         set { SetProperty(ref this.value, value, "Value"); }
      }

      public UserSetting()
      { }

      public UserSetting(string key, string displayText, object value = null)
      {
         this.key = key;
         this.displayText = displayText;
         this.value = value;
      }

      public override string ToString()
      {
         return this.DisplayText ?? this.Key;
      }
   }
}
