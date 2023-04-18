using System;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace SleepHunter.Win32
{
    internal static class NativeExtensions
    {
        public static DateTime FiletimeToDateTime(this FILETIME filetime)
        {
            ulong nanoSeconds = ((ulong)filetime.dwHighDateTime) << 32;
            nanoSeconds = unchecked(nanoSeconds | (uint)filetime.dwLowDateTime);

            var ticks = (long)nanoSeconds;

            return DateTime.FromFileTime(ticks);
        }

        public static TimeSpan FiletimeToTimeSpan(this FILETIME filetime)
        {
            ulong nanoSeconds = ((ulong)filetime.dwHighDateTime) << 32;
            nanoSeconds = unchecked(nanoSeconds | (uint)filetime.dwLowDateTime);

            var ticks = (long)nanoSeconds;

            return TimeSpan.FromTicks(ticks);
        }
    }
}
