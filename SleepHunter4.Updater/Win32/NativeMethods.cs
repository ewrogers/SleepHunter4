using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Updater.Win32
{
    internal static class NativeMethods
    {
        [DllImport("user32", EntryPoint = "SetForegroundWindow", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr windowHandle);
    }
}
