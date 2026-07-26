using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SleepHunter.Win32;

namespace SleepHunter.Macro
{
    internal interface IWindowHotkeyApi
    {
        bool Register(
            nint windowHandle,
            int hotkeyId,
            ModifierKeys modifiers,
            int virtualKey);

        bool Unregister(
            nint windowHandle,
            int hotkeyId);
    }

    internal sealed class WindowHotkeyApi :
        IWindowHotkeyApi
    {
        public bool Register(
            nint windowHandle,
            int hotkeyId,
            ModifierKeys modifiers,
            int virtualKey) =>
            NativeMethods.RegisterHotKey(
                windowHandle,
                hotkeyId,
                modifiers,
                virtualKey);

        public bool Unregister(
            nint windowHandle,
            int hotkeyId) =>
            NativeMethods.UnregisterHotKey(
                windowHandle,
                hotkeyId);
    }

    public sealed class HotkeyManager
    {
        internal const int MaximumApplicationHotkeyId = 0xBFFF;

        private static readonly HotkeyManager instance = new();
        public static HotkeyManager Instance => instance;

        private readonly ConcurrentDictionary<int, Hotkey> hotkeys = new();
        private readonly IWindowHotkeyApi windowHotkeys;
        private readonly object syncRoot = new();
        private int lastHotkeyId;

        private HotkeyManager()
            : this(new WindowHotkeyApi())
        {
        }

        internal HotkeyManager(
            IWindowHotkeyApi windowHotkeys)
        {
            this.windowHotkeys = windowHotkeys ??
                throw new ArgumentNullException(
                    nameof(windowHotkeys));
        }

        public IEnumerable<Hotkey> Hotkeys =>
            from hotkey in hotkeys.Values
            select hotkey;

        public bool RegisterHotkey(nint windowHandle, Hotkey hotkey)
        {
            if (hotkey == null)
                throw new ArgumentNullException(nameof(hotkey));

            lock (syncRoot)
            {
                if (hotkey.IsActive)
                    return false;

                var hotkeyId = AllocateHotkeyId();
                if (hotkeyId <= 0)
                    return false;

                var virtualKey =
                    KeyInterop.VirtualKeyFromKey(hotkey.Key);
                if (!windowHotkeys.Register(
                        windowHandle,
                        hotkeyId,
                        hotkey.Modifiers,
                        virtualKey))
                {
                    return false;
                }

                hotkey.Id = hotkeyId;
                hotkeys[hotkeyId] = hotkey;
                return true;
            }
        }

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

            lock (syncRoot)
            {
                if (!hotkey.IsActive ||
                    !hotkeys.TryGetValue(
                        hotkey.Id,
                        out var registeredHotkey) ||
                    !ReferenceEquals(
                        hotkey,
                        registeredHotkey))
                {
                    return false;
                }

                if (!windowHotkeys.Unregister(
                        windowHandle,
                        hotkey.Id))
                {
                    return false;
                }

                var removed =
                    hotkeys.TryRemove(
                        hotkey.Id,
                        out var removedHotkey) &&
                    ReferenceEquals(
                        hotkey,
                        removedHotkey);
                if (removed)
                    hotkey.Id = -1;

                return removed;
            }
        }

        public void UnregisterAllHotkeys(nint windowHandle)
        {
            foreach (var hotkey in hotkeys.Values)
                UnregisterHotkey(windowHandle, hotkey);
        }

        private int AllocateHotkeyId()
        {
            for (var attempt = 0;
                 attempt < MaximumApplicationHotkeyId;
                 attempt++)
            {
                lastHotkeyId =
                    lastHotkeyId >= MaximumApplicationHotkeyId
                        ? 1
                        : lastHotkeyId + 1;
                if (!hotkeys.ContainsKey(lastHotkeyId))
                    return lastHotkeyId;
            }

            return -1;
        }
    }
}
