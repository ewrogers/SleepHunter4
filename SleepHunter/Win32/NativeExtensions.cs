using System;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace SleepHunter.Win32
{
    internal static class NativeExtensions
    {
        public static DateTime FiletimeToDateTime(this FILETIME filetime)
        {
            var hi = (ulong)filetime.dwHighDateTime << 32;
            var lo = (ulong)filetime.dwLowDateTime;

            var fileTimeLong = unchecked(hi | lo);
            return DateTime.FromFileTime((long)fileTimeLong);
        }

        public static TimeSpan FiletimeToTimeSpan(this FILETIME filetime)
        {
            var hi = (ulong)filetime.dwHighDateTime << 32;
            var lo = (ulong)filetime.dwLowDateTime;

            var fileTimeLong = unchecked(hi | lo);
            return TimeSpan.FromTicks((long)fileTimeLong);
        }
    }
}
