using SleepHunter.Macro;

namespace SleepHunter.Services.Configuration
{
    public interface IHotkeyRegistrationService
    {
        bool Register(Hotkey hotkey);

        bool Unregister(Hotkey hotkey);
    }
}
