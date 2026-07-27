using SleepHunter.ViewModels.Editing;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Configuration
{
    public interface IClientMacroConfigurationMapper
    {
        MacroConfiguration CreateSnapshot(
            ClientMacroConfiguration source);

        void Apply(
            ClientMacroConfiguration destination,
            MacroConfigurationLoadResult loaded);
    }
}
