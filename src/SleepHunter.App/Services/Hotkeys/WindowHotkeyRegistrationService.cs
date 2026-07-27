using System;
using System.Windows.Input;
namespace SleepHunter.Services.Hotkeys
{
    public sealed class WindowHotkeyRegistrationService :
        IHotkeyRegistrationService
    {
        private readonly Func<nint> getWindowHandle;
        private readonly WindowHotkeyRegistry hotkeys;

        public WindowHotkeyRegistrationService(
            WindowHotkeyRegistry hotkeys,
            Func<nint> getWindowHandle)
        {
            this.hotkeys = hotkeys ??
                throw new ArgumentNullException(nameof(hotkeys));
            this.getWindowHandle = getWindowHandle ??
                throw new ArgumentNullException(
                    nameof(getWindowHandle));
        }

        public bool Register(Hotkey hotkey) =>
            hotkeys.RegisterHotkey(
                GetWindowHandle(),
                hotkey);

        public Hotkey Find(
            Key key,
            ModifierKeys modifiers) =>
            hotkeys.GetHotkey(
                key,
                modifiers);

        public bool Unregister(Hotkey hotkey) =>
            hotkeys.UnregisterHotkey(
                GetWindowHandle(),
                hotkey);

        public void UnregisterAll() =>
            hotkeys.UnregisterAllHotkeys(
                GetWindowHandle());

        private nint GetWindowHandle()
        {
            var windowHandle = getWindowHandle();
            if (windowHandle == 0)
            {
                throw new InvalidOperationException(
                    "The main window handle is unavailable.");
            }

            return windowHandle;
        }
    }
}
