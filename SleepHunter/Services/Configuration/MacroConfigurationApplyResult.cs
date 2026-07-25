using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Configuration
{
    public sealed record MacroConfigurationApplyResult(
        MacroConfigurationLoadResult Loaded,
        bool HotkeyRegistrationFailed);
}
