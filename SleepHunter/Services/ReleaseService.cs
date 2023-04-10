using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SleepHunter.Models;

namespace SleepHunter.Services
{
    public class ReleaseService : IReleaseService
    {
        private const string RELEASES_URL = @"https://api.github.com/repos/ewrogers/SleepHunter4/releases";
        private const string RELEASE_NOTES_URL = @"https://github.com/ewrogers/SleepHunter4/blob/main/CHANGELOG.md";
        private const int DOWNLOAD_BUFFER_SIZE = 16 * 1024;

        public static readonly Regex TagNameRegex = new Regex(@"""tag_name"":\s*""v(\d+)\.(\d+)\.(\d+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex AssetUrlRegex = new Regex(@"""assets"":\s*\[{.*""browser_download_url"":\s*""(.*)"".*}\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex AssetSizeRegex = new Regex(@"""assets"":\s*\[{.*""size"":\s*(\d+).*}\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly HttpClient client;
        
        public ReleaseService()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "SleepHunter v4 Client");
        }

        public Uri GetLatestReleaseNotesUri() => new Uri(RELEASE_NOTES_URL);

        public async Task<ReleaseInfo> GetLatestReleaseAsync()
        {
            var res = await client.GetStringAsync($"{RELEASES_URL}/latest");

            var tagMatch = TagNameRegex.Match(res);
            if (!tagMatch.Success)
                throw new InvalidOperationException("Unable to determine latest version");

            var major = int.Parse(tagMatch.Groups[1].Value);
            var minor = int.Parse(tagMatch.Groups[2].Value);
            var build = int.Parse(tagMatch.Groups[3].Value);

            var assetUrlMatch = AssetUrlRegex.Match(res);
            if (!assetUrlMatch.Success)
                throw new InvalidOperationException("Unable to determine donwload url");

            var downloadUri = new Uri(assetUrlMatch.Groups[1].Value);

            var assetSizeMatch = AssetSizeRegex.Match(res);
            if (!assetSizeMatch.Success)
                throw new InvalidOperationException("Unable to determine content size");

            var contentSize = int.Parse(assetSizeMatch.Groups[1].Value);

            return new ReleaseInfo(
                new Version(major, minor, build),
                downloadUri,
                contentSize);
        }

        public async Task<string> DownloadLatestReleaseAsync(IProgress<long> progress = null)
        {
            var release = await GetLatestReleaseAsync();
            var tempFilePath = Path.Combine(Path.GetTempPath(), "update.zip");

            var buffer = new byte[DOWNLOAD_BUFFER_SIZE];
            var totalBytesRead = 0;

            using (var downloadStream = await client.GetStreamAsync(release.DownloadUri))
            using (var outputStream = File.Create(tempFilePath, DOWNLOAD_BUFFER_SIZE))
            {
                while (downloadStream.CanRead)
                {
                    var bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        var percentage = (totalBytesRead * 100) / release.ContentSize;
                        progress?.Report(percentage);
                    }
                    else break;
                }

                outputStream.Flush();
            }

            return tempFilePath;
        }
    }
}
