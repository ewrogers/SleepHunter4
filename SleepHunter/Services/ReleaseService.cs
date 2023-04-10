using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SleepHunter.Models;

namespace SleepHunter.Services
{
    public class ReleaseService : IReleaseService
    {
        private const string RELEASES_URL = @"https://github.com/ewrogers/SleepHunter4/releases";
        private const string RELEASE_ASSETS_URL = @"https://api.github.com/repos/ewrogers/SleepHunter4/releases";
        private const string RELEASE_NOTES_URL = @"https://github.com/ewrogers/SleepHunter4/blob/main/CHANGELOG.md";
        private const int DOWNLOAD_BUFFER_SIZE = 16 * 1024;

        public static readonly Regex IdRegex = new Regex(@"""id"":\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex TagNameRegex = new Regex(@"""tag_name"":\s*""v(\d+)\.(\d+)\.(\d+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex DownloadUrlRegex = new Regex(@"""browser_download_url"":\s*""(.*)"".*}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex ContentSizeRegex = new Regex(@"""size"":\s*(\d+).*}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly HttpClient client;

        public ReleaseService()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "SleepHunter v4 Client");
        }

        public Uri GetLatestReleaseNotesUri() => new Uri(RELEASE_NOTES_URL);

        public async Task<ReleaseVersion> GetLatestReleaseVersionAsync()
        {
            var res = await client.GetAsync($"{RELEASES_URL}/latest");
            var body = await res.Content.ReadAsStringAsync();

            var idMatch = IdRegex.Match(body);
            if (!idMatch.Success)
                throw new InvalidOperationException("Unable to determine release ID");

            var releaseId = int.Parse(idMatch.Groups[1].Value);

            var tagMatch = TagNameRegex.Match(body);
            if (!tagMatch.Success)
                throw new InvalidOperationException("Unable to determine latest version");

            var major = int.Parse(tagMatch.Groups[1].Value);
            var minor = int.Parse(tagMatch.Groups[2].Value);
            var build = int.Parse(tagMatch.Groups[3].Value);

            return new ReleaseVersion(
                releaseId,
                new Version(major, minor, build));
        }

        public async Task<ReleaseAsset> GetLatestReleaseAsync()
        {
            var latestRelease = await GetLatestReleaseVersionAsync();

            var res = await client.GetAsync($"{RELEASE_ASSETS_URL}/{latestRelease.Id}/assets");
            var body = await res.Content.ReadAsStringAsync();

            var idMatch = IdRegex.Match(body);
            if (!idMatch.Success)
                throw new InvalidOperationException("Unable to determine asset ID");

            var assetId = int.Parse(idMatch.Groups[1].Value);

            var downloadUrlMatch = DownloadUrlRegex.Match(body);
            if (!downloadUrlMatch.Success)
                throw new InvalidOperationException("Unable to determine donwload url");

            var downloadUri = new Uri(downloadUrlMatch.Groups[1].Value);

            var contentSizeMatch = ContentSizeRegex.Match(body);
            if (!contentSizeMatch.Success)
                throw new InvalidOperationException("Unable to determine content size");

            var contentSize = int.Parse(contentSizeMatch.Groups[1].Value);

            return new ReleaseAsset(
                assetId,
                latestRelease.Version,
                downloadUri,
                contentSize);
        }

        public async Task<string> DownloadLatestReleaseAsync(Uri downloadUri, IProgress<long> progress = null)
        {
            var release = await GetLatestReleaseAsync();
            var filename = release.DownloadUri.Segments.Last().ToString();

            var tempFilePath = Path.Combine(Path.GetTempPath(), filename);

            var buffer = new byte[DOWNLOAD_BUFFER_SIZE];
            var totalBytesRead = 0;

            using (var downloadStream = await client.GetStreamAsync(downloadUri))
            using (var outputStream = File.Create(tempFilePath, DOWNLOAD_BUFFER_SIZE))
            {
                while (downloadStream.CanRead)
                {
                    var bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        progress?.Report(totalBytesRead);
                    }
                    else break;
                }

                outputStream.Flush();
            }

            return tempFilePath;
        }
    }
}
