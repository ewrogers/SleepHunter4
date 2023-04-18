using System;

namespace SleepHunter.Models
{
    internal class ReleaseVersion
    {
        public int Id { get; }
        public Version Version { get; }
        public string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";

        public ReleaseVersion(int id, Version version)
        {
            Id = id;
            Version = version;
        }

        public override string ToString() => VersionString;
    }
}
