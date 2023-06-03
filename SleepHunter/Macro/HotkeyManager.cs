using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Input;

using SleepHunter.Win32;

namespace SleepHunter.Macro
{
    public sealed class HotkeyManager
    {
        private static readonly HotkeyManager instance = new();
        public static HotkeyManager Instance => instance;

        private HotkeyManager() { }


        private readonly ConcurrentDictionary<int, Hotkey> hotkeys = new();

        public int Count => hotkeys.Count;

        public IEnumerable<Hotkey> Hotkeys => from h in hotkeys.Values select h;

        public bool RegisterHotkey(nint windowHandle, Hotkey hotkey)
        {
            if (hotkey == null)
                throw new ArgumentNullException(nameof(hotkey));

            hotkey.AtomName = GetHotkeyUniqueName(hotkey);
            hotkey.Id = NativeMethods.GlobalAddAtom(hotkey.AtomName);

            if (hotkey.Id <= 0)
                return false;

            var vkey = KeyInterop.VirtualKeyFromKey(hotkey.Key);
            var success = NativeMethods.RegisterHotKey(windowHandle, hotkey.Id, hotkey.Modifiers, vkey);

            if (success)
                hotkeys[hotkey.Id] = hotkey;

            return success;
        }

        public bool ContainsHotkey(Key key, ModifierKeys modifiers) => GetHotkey(key, modifiers) != null;

        public Hotkey GetHotkey(Key key, ModifierKeys modifiers)
        {
            foreach (var hotkey in hotkeys.Values)
                if (hotkey.Key == key && hotkey.Modifiers == modifiers)
                    return hotkey;

            return null;
        }

        public bool UnregisterHotkey(nint windowHandle, Hotkey hotkey)
        {
            if (hotkey == null)
                throw new ArgumentNullException(nameof(hotkey));

            NativeMethods.UnregisterHotKey(windowHandle, hotkey.Id);

            var wasRemoved = hotkeys.TryRemove(hotkey.Id, out var removedHotkey);

            if (wasRemoved && hotkey.Id > 0)
                NativeMethods.GlobalDeleteAtom((ushort)hotkey.Id);

            return removedHotkey != null;
        }

        public void UnregisterAllHotkeys(nint windowHandle)
        {
            foreach (var hotkey in hotkeys.Values)
                UnregisterHotkey(windowHandle, hotkey);

            hotkeys.Clear();
        }

        static string GetHotkeyUniqueName(Hotkey hotkey)
        {
            var hotkeyName = string.Format("{0}_{1}_{2}",
               Environment.CurrentManagedThreadId.ToString("X8"),
               hotkey.GetType().FullName,
               hotkey.ToString());

            return hotkeyName;
        }
    }
}
