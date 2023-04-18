using System;

namespace SleepHunter.Extensions
{
    internal static class VersionExtender
    {
        public static bool IsNewerThan(this Version current, Version otherVersion) => current.CompareTo(otherVersion) > 0;

        public static bool IsOlderThan(this Version current, Version otherVersion) => current.CompareTo(otherVersion) < 0;
    }
}
