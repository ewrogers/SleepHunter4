using System;
using System.Threading.Tasks;
using SleepHunter.Models;

namespace SleepHunter.Services
{
    public interface IReleaseService
    {
        Uri GetLatestReleaseNotesUri();

        Task<ReleaseInfo> GetLatestReleaseAsync();

        Task<string> DownloadLatestReleaseAsync(IProgress<long> progress = null);
    }
}
