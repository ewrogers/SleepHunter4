using System;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace SleepHunter.Win32
{
    internal static class NativeExtensions
    {
        public static DateTime FiletimeToDateTime(this FILETIME filetime)
        {
            var hi = (uint)filetime.dwHighDateTime << 32;
            var lo = (uint)filetime.dwLowDateTime;

            var fileTimeLong = unchecked((ulong)hi | lo);
            return DateTime.FromFileTime((long)fileTimeLong);
        }

        public static TimeSpan FiletimeToTimeSpan(this FILETIME filetime)
        {
            var hi = (uint)filetime.dwHighDateTime << 32;
            var lo = (uint)filetime.dwLowDateTime;

            var fileTimeLong = unchecked((ulong)hi |lo);
            return TimeSpan.FromTicks((long)fileTimeLong);
        }
    }
}
