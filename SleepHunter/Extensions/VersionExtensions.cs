using System;

namespace SleepHunter.Extensions
{
    public static class VersionExtensions
    {
        public static bool IsNewerThan(this Version current, Version otherVersion) => current.CompareTo(otherVersion) > 0;

        public static bool IsOlderThan(this Version current, Version otherVersion) => current.CompareTo(otherVersion) < 0;
    }
}
