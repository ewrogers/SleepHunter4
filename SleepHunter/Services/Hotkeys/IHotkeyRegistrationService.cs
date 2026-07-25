using System.Windows.Input;
using SleepHunter.Macro;

namespace SleepHunter.Services.Hotkeys
{
    public interface IHotkeyRegistrationService
    {
        Hotkey Find(
            Key key,
            ModifierKeys modifiers);

        bool Register(Hotkey hotkey);

        bool Unregister(Hotkey hotkey);
    }
}
