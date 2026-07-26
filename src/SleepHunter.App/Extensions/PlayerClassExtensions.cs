using SleepHunter.Models;

namespace SleepHunter.Extensions
{
    public static class PlayerClassExtensions
    {
        public static bool TryFromClientValue(byte value, out PlayerClass playerClass)
        {
            switch (value)
            {
                case 0:
                    playerClass = PlayerClass.Peasant;
                    return true;
                case 1:
                    playerClass = PlayerClass.Warrior;
                    return true;
                case 2:
                    playerClass = PlayerClass.Rogue;
                    return true;
                case 3:
                    playerClass = PlayerClass.Wizard;
                    return true;
                case 4:
                    playerClass = PlayerClass.Priest;
                    return true;
                case 5:
                    playerClass = PlayerClass.Monk;
                    return true;
                default:
                    playerClass = PlayerClass.Peasant;
                    return false;
            }
        }

        public static bool Includes(this PlayerClass allowedClasses, PlayerClass playerClass)
        {
            if (allowedClasses == PlayerClass.All)
                return true;

            if (playerClass == PlayerClass.Peasant)
                return allowedClasses == PlayerClass.Peasant;

            return (allowedClasses & playerClass) == playerClass;
        }
    }
}
