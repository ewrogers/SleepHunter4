using SleepHunter.Macro;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Configuration
{
    public interface IPlayerMacroConfigurationMapper
    {
        MacroConfiguration CreateSnapshot(
            PlayerMacroConfiguration source);

        void Apply(
            PlayerMacroConfiguration destination,
            MacroConfigurationLoadResult loaded);
    }
}
