using System;
using System.ComponentModel;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Models;

namespace SleepHunter.Metadata
{
    [Serializable]
   public sealed class SkillMetadata : ObservableObject
    {
      string name;
      PlayerClass playerClass;
      string groupName;
      int manaCost;
      bool isAssail;
      bool opensDialog;
      bool canImprove = true;
      TimeSpan cooldown;
      bool requiresDisarm;

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
         set { SetProperty(ref manaCost, value, "Mana"); }
      }

      [XmlAttribute("IsAssail")]
      [DefaultValue(false)]
      public bool IsAssail
      {
         get { return isAssail; }
         set { SetProperty(ref isAssail, value, "IsAssail"); }
      }

      [XmlAttribute("OpensDialog")]
      [DefaultValue(false)]
      public bool OpensDialog
      {
         get { return opensDialog; }
         set { SetProperty(ref opensDialog, value, "OpensDialog"); }
      }

      [XmlAttribute("CanImprove")]
      [DefaultValue(true)]
      public bool CanImprove
      {
         get { return canImprove; }
         set { SetProperty(ref canImprove, value, "CanImprove"); }
      }

      [XmlIgnore]
      public TimeSpan Cooldown
      {
         get { return cooldown; }
         set { SetProperty(ref cooldown, value, "Cooldown", onChanged: (s) => { OnPropertyChanged("CooldownSeconds"); }); }
      }

      [XmlAttribute("Cooldown")]
      [DefaultValue(0.0)]
      public double CooldownSeconds
      {
         get { return cooldown.TotalSeconds; }
         set { this.Cooldown = TimeSpan.FromSeconds(value); }
      }

      [XmlAttribute("RequiresDisarm")]
      [DefaultValueAttribute(false)]
      public bool RequiresDisarm
      {
         get { return requiresDisarm; }
         set { SetProperty(ref requiresDisarm, value, "RequiresDisarm"); }
      }

      public SkillMetadata()
      {

      }

      public override string ToString()
      {
         return this.Name ?? "Unknown Skill";
      }

      public void CopyTo(SkillMetadata other)
      {
         other.Name = this.Name;
         other.Class = this.Class;
         other.GroupName = this.GroupName;
         other.ManaCost = this.ManaCost;
         other.Cooldown = this.Cooldown;
         other.IsAssail = this.IsAssail;
         other.OpensDialog = this.OpensDialog;
         other.CanImprove = this.CanImprove;
         other.RequiresDisarm = this.RequiresDisarm;
      }
   }
}
