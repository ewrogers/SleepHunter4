using SleepHunter.Models;

namespace SleepHunter.Extensions
{
    public static class PlayerClassExtensions
    {
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
