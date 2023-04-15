using System;
using System.Threading.Tasks;
using SleepHunter.Models;

namespace SleepHunter.Services.Releases
{
    public interface IReleaseService
    {
        Uri GetLatestReleaseNotesUri();

        Task<ReleaseVersion> GetLatestReleaseVersionAsync();

        Task<ReleaseAsset> GetLatestReleaseAsync();

        Task<string> DownloadLatestReleaseAsync(Uri downloadUri, IProgress<long> progress = null);
    }
}
