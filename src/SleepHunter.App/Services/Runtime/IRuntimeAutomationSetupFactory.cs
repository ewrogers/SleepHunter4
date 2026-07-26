using SleepHunter.Persistence.Configuration;
using SleepHunter.Runtime.Characters;
using SleepHunter.Settings;

namespace SleepHunter.Services.Runtime
{
    public interface IRuntimeAutomationSetupFactory
    {
        RuntimeAutomationSetup Create(
            MacroConfiguration configuration,
            UserSettings settings,
            CharacterClass characterClass);
    }
}
