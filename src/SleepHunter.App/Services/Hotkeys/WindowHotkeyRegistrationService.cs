using System;
using System.Windows.Input;
using SleepHunter.Macro;

namespace SleepHunter.Services.Hotkeys
{
    public sealed class WindowHotkeyRegistrationService :
        IHotkeyRegistrationService
    {
        private readonly Func<nint> getWindowHandle;

        public WindowHotkeyRegistrationService(
            Func<nint> getWindowHandle)
        {
            this.getWindowHandle = getWindowHandle ??
                throw new ArgumentNullException(
                    nameof(getWindowHandle));
        }

        public bool Register(Hotkey hotkey) =>
            HotkeyManager.Instance.RegisterHotkey(
                GetWindowHandle(),
                hotkey);

        public Hotkey Find(
            Key key,
            ModifierKeys modifiers) =>
            HotkeyManager.Instance.GetHotkey(
                key,
                modifiers);

        public bool Unregister(Hotkey hotkey) =>
            HotkeyManager.Instance.UnregisterHotkey(
                GetWindowHandle(),
                hotkey);

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
