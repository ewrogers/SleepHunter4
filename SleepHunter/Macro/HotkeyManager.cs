using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;

using SleepHunter.Win32;

namespace SleepHunter.Macro
{
   public sealed class HotkeyManager
   {
      #region Singleton
      static readonly HotkeyManager instance = new HotkeyManager();

      public static HotkeyManager Instance { get { return instance; } }

      private HotkeyManager() { }
      #endregion

      ConcurrentDictionary<int, Hotkey> hotkeys = new ConcurrentDictionary<int, Hotkey>();

      public int Count { get { return hotkeys.Count; } }

      public IEnumerable<Hotkey> Hotkeys
      {
         get { return from h in hotkeys.Values select h; }
      }

      public bool RegisterHotkey(IntPtr windowHandle, Hotkey hotkey)
      {
         if (hotkey == null)
            throw new ArgumentNullException("hotkey");

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

      public bool ContainsHotkey(Key key, ModifierKeys modifiers)
      {
         return GetHotkey(key, modifiers) != null;
      }

      public Hotkey GetHotkey(Key key, ModifierKeys modifiers)
      {
         foreach (var hotkey in hotkeys.Values)
            if (hotkey.Key == key && hotkey.Modifiers == modifiers)
               return hotkey;

         return null;
      }

      public bool UnregisterHotkey(IntPtr windowHandle, Hotkey hotkey)
      {
         if (hotkey == null)
            throw new ArgumentNullException("hotkey");

         var success = NativeMethods.UnregisterHotKey(windowHandle, hotkey.Id);

         Hotkey removedHotkey;
         var wasRemoved = hotkeys.TryRemove(hotkey.Id, out removedHotkey);

         if (wasRemoved && hotkey.Id > 0)
            NativeMethods.GlobalDeleteAtom((ushort)hotkey.Id);

         return removedHotkey != null;
      }

      public void UnregisterAllHotkeys(IntPtr windowHandle)
      {
         foreach (var hotkey in hotkeys.Values)
            UnregisterHotkey(windowHandle, hotkey);

         hotkeys.Clear();
      }

      static string GetHotkeyUniqueName(Hotkey hotkey)
      {
         var hotkeyName = string.Format("{0}_{1}_{2}",
            Thread.CurrentThread.ManagedThreadId.ToString("X8"),
            hotkey.GetType().FullName,
            hotkey.ToString());

         return hotkeyName;
      }
   }
}
