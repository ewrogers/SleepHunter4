using System;
using System.Windows;
using SleepHunter.Models;
using SleepHunter.Services.Releases;

namespace SleepHunter.Views
{
    public partial class UpdateProgressWindow : Window
    {
        private readonly IReleaseService releaseService;

        public bool ShouldInstall { get; private set; }
        public ReleaseAsset ReleaseInfo { get; private set; }
        public string DownloadPath { get; private set; }

        public UpdateProgressWindow()
        {
            releaseService = App.Current.Services.GetService<IReleaseService>();

            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            installButton.IsEnabled = false;

            SetStatusText("Fetching Update...");
            UpdateProgress(0);

            try
            {
                ReleaseInfo = await releaseService.GetLatestReleaseAsync();
                UpdateProgress(0);
            }
            catch (Exception ex)
            {
                SetStatusText("Unable to Fetch!");
                progressSizeText.Text = ex.Message;
                return;
            }

            try
            {
                SetStatusText($"Downloading version {ReleaseInfo.VersionString}...");
                var progressReporter = new Progress<long>(UpdateProgress);
                DownloadPath = await releaseService.DownloadLatestReleaseAsync(ReleaseInfo.DownloadUri, progressReporter);
            } 
            catch (Exception ex)
            {
                SetStatusText("Unable to Fetch!");
                progressSizeText.Text = ex.Message;
                return;
            }

            SetStatusText("Ready to Install");
            installButton.Content = $"_Install {ReleaseInfo.VersionString}";
            installButton.FontWeight = FontWeights.Normal;
            installButton.IsEnabled = true;
            installButton.Focus();
        }

        private void SetStatusText(string text) => statusText.Text = text;

        private void UpdateProgress(long downloadedSize)
        {
            var contentSize = ReleaseInfo?.ContentSize ?? 0;
            var percentage = contentSize > 0 ? downloadedSize * 100 / contentSize : 0;

            progressBar.Value = Math.Max(0, Math.Min(percentage, 100));
            progressPercentText.Text = $"{percentage}%";

            var downloadedKb = Math.Ceiling(downloadedSize / 1024.0);
            var contentKb = Math.Ceiling(contentSize / 1024.0);

            if (downloadedSize >= contentSize)
                progressSizeText.Text = $"{contentKb:0} KB"; 
            else if (contentSize > 0)
                progressSizeText.Text = $"{downloadedKb:0} KB / {contentKb:0} KB";
            else
                progressSizeText.Text = string.Empty;
        }

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldInstall = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldInstall = false;
            Close();
        }
    }
}
