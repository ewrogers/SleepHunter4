using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;

using SleepHunter.Models;

namespace SleepHunter.Converters
{
    public sealed class CharacterClassFlagsConverter :
        IValueConverter
    {
        private static readonly Regex WarriorRegex = new(@"\s*Warrior\s*");
        private static readonly Regex WizardRegex = new(@"\s*Wizard\s*");
        private static readonly Regex PriestRegex = new(@"\s*Priest\s*");
        private static readonly Regex RogueRegex = new(@"\s*Rogue\s*");
        private static readonly Regex MonkRegex = new(@"\s*Monk\s*");
        private static readonly Regex GladiatorRegex = new(@"\s*Gladiator\s*");
        private static readonly Regex SummonerRegex = new(@"\s*Summoner\s*");
        private static readonly Regex BardRegex = new(@"\s*Bard\s*");
        private static readonly Regex ArcherRegex = new(@"\s*Archer\s*");
        private static readonly Regex DruidRegex = new(@"\s*Druid\s*");
        private static readonly Regex AllClassRegex = new(@"\s*All\s*");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isMedenia = string.Equals("Medenia", parameter as string, StringComparison.OrdinalIgnoreCase);

            var playerClass = (CharacterClassFlags)value;

            if (playerClass == CharacterClassFlags.Peasant)
                return "Peasant";

            if (playerClass == CharacterClassFlags.All)
                return "All";

            if (playerClass == CharacterClassFlags.Warrior)
                return isMedenia ? "Gladiator" : "Warrior";

            if (playerClass == CharacterClassFlags.Wizard)
                return isMedenia ? "Summoner" : "Wizard";

            if (playerClass == CharacterClassFlags.Priest)
                return isMedenia ? "Bard" : "Priest";

            if (playerClass == CharacterClassFlags.Rogue)
                return isMedenia ? "Archer" : "Rogue";

            if (playerClass == CharacterClassFlags.Monk)
                return isMedenia ? "Druid" : "Monk";

            var sb = new StringBuilder();

            if (playerClass.HasFlag(CharacterClassFlags.Warrior))
                if (sb.Length == 0)
                    sb.Append(isMedenia ? "Gladiator" : "Warrior");
                else
                    sb.Append(isMedenia ? " / Gladiator" : " / Warrior");

            if (playerClass.HasFlag(CharacterClassFlags.Wizard))
                if (sb.Length == 0)
                    sb.Append(isMedenia ? "Summoner" : "Wizard");
                else
                    sb.Append(isMedenia ? " / Summoner" : " / Wizard");

            if (playerClass.HasFlag(CharacterClassFlags.Priest))
                if (sb.Length == 0)
                    sb.Append(isMedenia ? "Bard" : "Priest");
                else
                    sb.Append(isMedenia ? " / Bard" : " / Priest");

            if (playerClass.HasFlag(CharacterClassFlags.Rogue))
                if (sb.Length == 0)
                    sb.Append(isMedenia ? "Archer" : "Rogue");
                else
                    sb.Append(isMedenia ? " / Archer" : " / Rogue");

            if (playerClass.HasFlag(CharacterClassFlags.Monk))
                if (sb.Length == 0)
                    sb.Append(isMedenia ? "Druid" : "Monk");
                else
                    sb.Append(isMedenia ? " / Druid" : " / Monk");

            return sb.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isMedenia = string.Equals("Medenia", parameter as string, StringComparison.OrdinalIgnoreCase);
            var playerClass = CharacterClassFlags.Peasant;

            if (value is not string valueString)
                return playerClass;

            if (AllClassRegex.IsMatch(valueString))
                return CharacterClassFlags.All;

            if (WarriorRegex.IsMatch(valueString) || (isMedenia && GladiatorRegex.IsMatch(valueString)))
                playerClass |= CharacterClassFlags.Warrior;

            if (WizardRegex.IsMatch(valueString) || (isMedenia && SummonerRegex.IsMatch(valueString)))
                playerClass |= CharacterClassFlags.Wizard;

            if (PriestRegex.IsMatch(valueString) || (isMedenia && BardRegex.IsMatch(valueString)))
                playerClass |= CharacterClassFlags.Priest;

            if (RogueRegex.IsMatch(valueString) || (isMedenia && ArcherRegex.IsMatch(valueString)))
                playerClass |= CharacterClassFlags.Rogue;

            if (MonkRegex.IsMatch(valueString) || (isMedenia && DruidRegex.IsMatch(valueString)))
                playerClass |= CharacterClassFlags.Monk;

            return playerClass;
        }
    }
}
