using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;

using SleepHunter.Models;

namespace SleepHunter.Converters
{
    public sealed class PlayerClassConverter : IValueConverter
    {
        private static readonly Regex WarriorRegex = new Regex(@"\s*Warrior\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WizardRegex = new Regex(@"\s*Wizard\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PriestRegex = new Regex(@"\s*Priest\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RogueRegex = new Regex(@"\s*Rogue\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MonkRegex = new Regex(@"\s*Monk\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GladiatorRegex = new Regex(@"\s*Gladiator\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SummonerRegex = new Regex(@"\s*Summoner\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BardRegex = new Regex(@"\s*Bard\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ArcherRegex = new Regex(@"\s*Archer\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DruidRegex = new Regex(@"\s*Druid\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex AllClassRegex = new Regex(@"\s*All\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public const string MedeniaParameter = "Medenia";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isMedenia = string.Equals(MedeniaParameter, parameter as string, StringComparison.OrdinalIgnoreCase);

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
            var isMedenia = string.Equals(MedeniaParameter, parameter as string, StringComparison.OrdinalIgnoreCase);

            var playerClass = PlayerClass.Peasant;

            if (!(value is string valueString))
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
