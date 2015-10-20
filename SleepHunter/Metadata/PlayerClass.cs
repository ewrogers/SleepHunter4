using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using System.Threading.Tasks;
using System.Windows.Data;

namespace SleepHunter.Metadata
{
   [Flags]
   public enum PlayerClass
   {
      Peasant = 0,
      Warrior = 0x1,
      Wizard = 0x2,
      Priest = 0x4,
      Rogue = 0x8,
      Monk = 0x10,
      All = Warrior | Wizard | Priest | Rogue | Monk
   }

   public sealed class PlayerClassConverter : IValueConverter
   {
      static readonly Regex WarriorRegex = new Regex(@"\s*Warrior\s*");
      static readonly Regex WizardRegex = new Regex(@"\s*Wizard\s*");
      static readonly Regex PriestRegex = new Regex(@"\s*Priest\s*");
      static readonly Regex RogueRegex = new Regex(@"\s*Rogue\s*");
      static readonly Regex MonkRegex = new Regex(@"\s*Monk\s*");
      static readonly Regex GladiatorRegex = new Regex(@"\s*Gladiator\s*");
      static readonly Regex SummonerRegex = new Regex(@"\s*Summoner\s*");
      static readonly Regex BardRegex = new Regex(@"\s*Bard\s*");
      static readonly Regex ArcherRegex = new Regex(@"\s*Archer\s*");
      static readonly Regex DruidRegex = new Regex(@"\s*Druid\s*");
      static readonly Regex AllClassRegex = new Regex(@"\s*All\s*");

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         var isMedenia = string.Equals("Medenia", parameter as string, StringComparison.OrdinalIgnoreCase);

         var playerClass = (PlayerClass)value;

         if (playerClass == PlayerClass.Peasant)
            return "Peasant";

         if (playerClass == PlayerClass.All)
            return "All";

         if (playerClass == PlayerClass.Warrior)
            return isMedenia ? "Gladiator" : "Warrior";

         if (playerClass == PlayerClass.Wizard)
            return isMedenia ? "Summoner" : "Wizard";

         if (playerClass == PlayerClass.Priest)
            return isMedenia ? "Bard" : "Priest";

         if (playerClass == PlayerClass.Rogue)
            return isMedenia ? "Archer" : "Rogue";

         if (playerClass == PlayerClass.Monk)
            return isMedenia ? "Druid" : "Monk";

         var sb = new StringBuilder();

         if (playerClass.HasFlag(PlayerClass.Warrior))
            if (sb.Length == 0)
               sb.Append(isMedenia ? "Gladiator" : "Warrior");
            else
               sb.Append(isMedenia ? " / Gladiator" : " / Warrior");

         if (playerClass.HasFlag(PlayerClass.Wizard))
            if (sb.Length == 0)
               sb.Append(isMedenia ? "Summoner" : "Wizard");
            else
               sb.Append(isMedenia ? " / Summoner" : " / Wizard");

         if (playerClass.HasFlag(PlayerClass.Priest))
            if (sb.Length == 0)
               sb.Append(isMedenia ? "Bard" : "Priest");
            else
               sb.Append(isMedenia ? " / Bard" : " / Priest");

         if (playerClass.HasFlag(PlayerClass.Rogue))
            if (sb.Length == 0)
               sb.Append(isMedenia ? "Archer" : "Rogue");
            else
               sb.Append(isMedenia ? " / Archer" : " / Rogue");

         if (playerClass.HasFlag(PlayerClass.Monk))
            if (sb.Length == 0)
               sb.Append(isMedenia ? "Druid" : "Monk");
            else
               sb.Append(isMedenia ? " / Druid" : " / Monk");

         return sb.ToString();
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         var isMedenia = string.Equals("Medenia", parameter as string, StringComparison.OrdinalIgnoreCase);

         var valueString = value as string;

         var playerClass = PlayerClass.Peasant;

         if (valueString == null)
            return playerClass;

         if (AllClassRegex.IsMatch(valueString))
            return PlayerClass.All;

         if (WarriorRegex.IsMatch(valueString) || (isMedenia && GladiatorRegex.IsMatch(valueString)))
            playerClass |= PlayerClass.Warrior;

         if (WizardRegex.IsMatch(valueString) || (isMedenia && SummonerRegex.IsMatch(valueString)))
            playerClass |= PlayerClass.Wizard;

         if (PriestRegex.IsMatch(valueString) || (isMedenia && BardRegex.IsMatch(valueString)))
            playerClass |= PlayerClass.Priest;

         if (RogueRegex.IsMatch(valueString) || (isMedenia && ArcherRegex.IsMatch(valueString)))
            playerClass |= PlayerClass.Rogue;

         if (MonkRegex.IsMatch(valueString) || (isMedenia && DruidRegex.IsMatch(valueString)))
            playerClass |= PlayerClass.Monk;

         return playerClass;
      }
   }
}
