using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

using SleepHunter.Media;
using SleepHunter.Settings;

namespace SleepHunter.Data
{
   public delegate void AbilityCallback(Ability ability);

   public abstract class Ability : NotifyObject
   {
      static readonly Regex TrimLevelRegex = new Regex(@"^(?<name>.*)\(Lev:(?<current>[0-9]{1,})/(?<max>[0-9]{1,})\)$");

      bool isEmpty;
      int slot;
      InterfacePanel panel;
      string name;
      int iconIndex;
      ImageSource icon;
      TimeSpan cooldown;
      bool isOnCooldown;
      int currentLevel;
      int maximumLevel;
      int numberOfLines;
      int manaCost;
      bool canImprove;
      bool isActive;

      public bool IsEmpty
      {
         get { return isEmpty; }
         set { SetProperty(ref isEmpty, value, "IsEmpty"); }
      }

      public int Slot
      {
         get { return slot; }
         set
         {
            SetProperty(ref slot, value, "Slot", onChanged: (s) => { OnPropertyChanged("RelativeSlot"); });
         }
      }

      public int RelativeSlot
      {
         get { return slot % 36; }
      }

      public InterfacePanel Panel
      {
         get { return panel; }
         set
         {
            SetProperty(ref panel, value, "Panel", onChanged: (s) => { OnPropertyChanged("IsSkill"); OnPropertyChanged("IsSpell"); });
         }
      }

      public bool IsSkill
      {
         get { return panel.IsSkillPanel(); }
      }

      public bool IsSpell
      {
         get { return panel.IsSpellPanel(); }
      }

      public bool IsActive
      {
         get { return isActive; }
         set { SetProperty(ref isActive, value, "IsActive"); }
      }

      public string Name
      {
         get { return name; }
         set { SetProperty(ref name, value, "Name"); }
      }

      public int IconIndex
      {
         get { return iconIndex; }
         set { SetProperty(ref iconIndex, value, "IconIndex"); }
      }

      public ImageSource Icon
      {
         get { return icon; }
         set { SetProperty(ref icon, value, "Icon"); }
      }

      public bool IsOnCooldown
      {
         get { return isOnCooldown; }
         set { SetProperty(ref isOnCooldown, value, "IsOnCooldown"); }
      }

      public TimeSpan Cooldown
      {
         get { return cooldown; }
         set { SetProperty(ref cooldown, value, "Cooldown"); }
      }

      public int CurrentLevel
      {
         get { return currentLevel; }
         set { SetProperty(ref currentLevel, value, "CurrentLevel"); }
      }

      public int MaximumLevel
      {
         get { return maximumLevel; }
         set { SetProperty(ref maximumLevel, value, "MaximumLevel"); }
      }

      public int NumberOfLines
      {
         get { return numberOfLines; }
         set { SetProperty(ref numberOfLines, value, "NumberOfLines"); }
      }

      public int ManaCost
      {
         get { return manaCost; }
         set { SetProperty(ref manaCost, value, "ManaCost"); }
      }

      public bool CanImprove
      {
         get { return canImprove; }
         set { SetProperty(ref canImprove, value, "CanImprove"); }
      }

      public static InterfacePanel GetSkillPanelForSlot(int slot)
      {
         if (slot <= 36)
            return InterfacePanel.TemuairSkills;

         if (slot <= 72)
            return InterfacePanel.MedeniaSkills;

         return InterfacePanel.WorldSkills;
      }

      public static InterfacePanel GetSpellPanelForSlot(int slot)
      {
         if (slot <= 36)
            return InterfacePanel.TemuairSpells;

         if (slot <= 72)
            return InterfacePanel.MedeniaSpells;

         return InterfacePanel.WorldSpells;
      }
      
      public static bool TryParseLevels(string skillSpellText, out string name, out int currentLevel, out int maximumLevel)
      {
         name = null;
         currentLevel = 0;
         maximumLevel = 0;

         var match = TrimLevelRegex.Match(skillSpellText);

         if (!match.Success)
            return false;

         name = match.Groups["name"].Value.Trim();
         int.TryParse(match.Groups["current"].Value, out currentLevel);
         int.TryParse(match.Groups["max"].Value, out maximumLevel);

         return true;
      }
   }
}
