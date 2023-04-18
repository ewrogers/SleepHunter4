using System;

namespace SleepHunter.Models
{
    public sealed class ReleaseAsset
    {
        public int Id { get; }

        public Version Version { get; }
        public string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";
        
        public Uri DownloadUri { get; }

        public long ContentSize { get; }

        public ReleaseAsset(int id, Version version, Uri downloadUri, long contentSize)
        {
            Id = id;
            Version = version;
            DownloadUri = downloadUri;
            ContentSize = contentSize;
        }

        public override string ToString() => VersionString;
    }
}
