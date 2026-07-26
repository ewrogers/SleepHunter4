using System.Windows.Input;
using SleepHunter.Macro;

namespace SleepHunter.Services.Hotkeys
{
    internal enum HotkeyInputKind
    {
        Ignore,
        Assign,
        Clear
    }

    internal sealed record HotkeyInput(
        HotkeyInputKind Kind,
        Hotkey Hotkey = null);

    internal static class HotkeyInputParser
    {
        private const ModifierKeys SupportedModifiers =
            ModifierKeys.Alt |
            ModifierKeys.Control |
            ModifierKeys.Shift |
            ModifierKeys.Windows;

        public static HotkeyInput Parse(
            Key key,
            Key systemKey,
            ModifierKeys modifiers)
        {
            var resolvedKey =
                key == Key.System
                    ? systemKey
                    : key;
            if (resolvedKey == Key.None ||
                IsModifierKey(resolvedKey))
            {
                return new HotkeyInput(
                    HotkeyInputKind.Ignore);
            }

            var supportedModifiers =
                modifiers & SupportedModifiers;
            if (supportedModifiers == ModifierKeys.None &&
                resolvedKey is
                    Key.Delete or
                    Key.Back or
                    Key.Escape)
            {
                return new HotkeyInput(
                    HotkeyInputKind.Clear);
            }

            if (supportedModifiers == ModifierKeys.None &&
                !Hotkey.IsFunctionKey(resolvedKey))
            {
                return new HotkeyInput(
                    HotkeyInputKind.Ignore);
            }

            return new HotkeyInput(
                HotkeyInputKind.Assign,
                new Hotkey(
                    supportedModifiers,
                    resolvedKey));
        }

        private static bool IsModifierKey(Key key) =>
            key is
                Key.LeftCtrl or
                Key.RightCtrl or
                Key.LeftAlt or
                Key.RightAlt or
                Key.LeftShift or
                Key.RightShift or
                Key.LWin or
                Key.RWin;
    }
}
