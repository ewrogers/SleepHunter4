using System;
using System.Threading.Tasks;

namespace SleepHunter.Services
{
    public interface IReleaseService
    {
        Uri GetLatestReleaseNotesUri();

        Task<Version> GetLatestReleaseVersionAsync();

        Task<Uri> GetLatestReleaseDownloadUriAsync();
    }
}
