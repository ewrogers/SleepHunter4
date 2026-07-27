using SleepHunter.Models;

namespace SleepHunter.Extensions
{
    public static class CharacterClassFlagsExtensions
    {
        public static bool Includes(this CharacterClassFlags allowedClasses, CharacterClassFlags playerClass)
        {
            if (allowedClasses == CharacterClassFlags.All)
                return true;

            if (playerClass == CharacterClassFlags.Peasant)
                return allowedClasses == CharacterClassFlags.Peasant;

            return (allowedClasses & playerClass) == playerClass;
        }
    }
}
