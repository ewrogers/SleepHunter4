using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SleepHunter.Services
{
    public class ReleaseService : IReleaseService
    {
        private const string RELEASES_URL = @"https://api.github.com/repos/ewrogers/SleepHunter4/releases";
        private const string RELEASE_NOTES_URL = @"https://github.com/ewrogers/SleepHunter4/blob/main/CHANGELOG.md";

        public static readonly Regex TagNameRegex = new Regex(@"""tag_name"":\s*""v(\d+)\.(\d+)\.(\d+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex AssetUrlRegex = new Regex(@"""assets"":\s*\[{.*""browser_download_url"":\s*""(.*)"".*}\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly HttpClient client;

        public ReleaseService()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "SleepHunter v4 Client");
        }

        public Uri GetLatestReleaseNotesUri() => new Uri(RELEASE_NOTES_URL);

        public async Task<Version> GetLatestReleaseVersionAsync()
        {
            var res = await client.GetStringAsync($"{RELEASES_URL}/latest");

            var match = TagNameRegex.Match(res);
            if (!match.Success)
                throw new InvalidOperationException("Unable to determine latest version");

            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var build = int.Parse(match.Groups[3].Value);

            return new Version(major, minor, build);
        }

        public async Task<Uri> GetLatestReleaseDownloadUriAsync()
        {
            var res = await client.GetStringAsync($"{RELEASES_URL}/latest");

            var match = AssetUrlRegex.Match(res);
            if (!match.Success)
                throw new InvalidOperationException("Unable to get download uri");

            var uriString = match.Groups[1].Value;
            return new Uri(uriString);
        }
    }
}
