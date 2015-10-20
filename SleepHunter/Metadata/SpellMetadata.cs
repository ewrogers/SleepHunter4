using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using SleepHunter.Data;

namespace SleepHunter.Metadata
{
   [Serializable]
   public sealed class SpellMetadata : NotifyObject
   {
      string name;
      PlayerClass playerClass;
      string groupName;
      int manaCost;
      int numberOfLines;
      TimeSpan cooldown;
      bool canImprove = true;

      [XmlAttribute("Name")]
      public string Name
      {
         get { return name; }
         set { SetProperty(ref name, value, "Name"); }
      }

      [XmlAttribute("Class")]
      [DefaultValue(PlayerClass.Peasant)]
      public PlayerClass Class
      {
         get { return playerClass; }
         set { SetProperty(ref playerClass, value, "Class"); }
      }

      [XmlAttribute("Group")]
      [DefaultValue(null)]
      public string GroupName
      {
         get { return groupName; }
         set { SetProperty(ref groupName, value, "GroupName"); }
      }

      [XmlAttribute("Mana")]
      [DefaultValue(0)]
      public int ManaCost
      {
         get { return manaCost; }
         set { SetProperty(ref manaCost, value, "ManaCost"); }
      }

      [XmlAttribute("Lines")]
      [DefaultValue(0)]
      public int NumberOfLines
      {
         get { return numberOfLines; }
         set { SetProperty(ref numberOfLines, value, "NumberOfLines"); }
      }

      [XmlIgnore]
      public TimeSpan Cooldown
      {
         get { return cooldown; }
         set { SetProperty(ref cooldown, value, "Cooldown"); }
      }

      [XmlAttribute("Cooldown")]
      [DefaultValue(0.0)]
      public double CooldownSeconds
      {
         get { return cooldown.TotalSeconds; }
         set { this.Cooldown = TimeSpan.FromSeconds(value); }
      }

      [XmlAttribute("CanImprove")]
      [DefaultValue(true)]
      public bool CanImprove
      {
         get { return canImprove; }
         set { SetProperty(ref canImprove, value, "CanImprove"); }
      }

      public SpellMetadata()
      {

      }

      public override string ToString()
      {
         return this.Name ?? "Unknown Spell";
      }

      public void CopyTo(SpellMetadata other)
      {
         other.Name = this.Name;
         other.Class = this.Class;
         other.GroupName = this.GroupName;
         other.ManaCost = this.ManaCost;
         other.NumberOfLines = this.NumberOfLines;
         other.Cooldown = this.Cooldown;         
         other.CanImprove = this.CanImprove;
      }
   }
}
