using System;

namespace SleepHunter.Models
{
    public sealed class ReleaseInfo
    {
        public Version Version { get; }
        public string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";
        
        public Uri DownloadUri { get; }

        public long ContentSize { get; }

        public ReleaseInfo(Version version, Uri downloadUri, long contentSize)
        {
            Version = version;
            DownloadUri = downloadUri;
            ContentSize = contentSize;
        }

        public override string ToString() => VersionString;
    }
}
