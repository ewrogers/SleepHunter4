using SleepHunter.Macro;
using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Runtime
{
    public interface IRuntimeMacroConfigurationAdapter
    {
        MacroConfigurationLoadResult Adapt(
            PlayerMacroConfiguration configuration);
    }
}
