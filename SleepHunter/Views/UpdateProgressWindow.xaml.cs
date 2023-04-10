﻿using System;
using System.Windows;
using SleepHunter.Models;
using SleepHunter.Services;

namespace SleepHunter.Views
{
    public partial class UpdateProgressWindow : Window
    {
        private readonly IReleaseService releaseService = new ReleaseService();

        public bool ShouldInstall { get; private set; }
        public ReleaseInfo ReleaseInfo { get; private set; }
        public string DownloadPath { get; private set; }

        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        async void Window_Loaded(object sender, RoutedEventArgs e)
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
                DownloadPath = await releaseService.DownloadLatestReleaseAsync(progressReporter);
            } 
            catch (Exception ex)
            {
                SetStatusText("Unable to Fetch!");
                progressSizeText.Text = ex.Message;
                return;
            }

            SetStatusText("Ready to Install");
            installButton.Content = $"_Install v{ReleaseInfo.VersionString}";
            installButton.FontWeight = FontWeights.Normal;
            installButton.IsEnabled = true;
            installButton.Focus();
        }

        void SetStatusText(string text) => statusText.Text = text;

        void UpdateProgress(long downloadedSize)
        {
            var contentSize = ReleaseInfo?.ContentSize ?? 0;
            var percentage = contentSize > 0 ? downloadedSize * 100 / contentSize : 0;

            progressBar.Value = Math.Max(0, Math.Min(percentage, 100));
            progressPercentText.Text = $"{percentage}%";

            if (contentSize > 0) {
                var downloadedKb = downloadedSize / 1024.0;
                var contentKb = contentSize / 1024.0;
                progressSizeText.Text = $"{downloadedKb:0.0} KB / {contentKb:0.0} KB";
            }
            else progressSizeText.Text = string.Empty;
        }

        void installButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldInstall = true;
            Close();
        }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldInstall = false;
            Close();
        }
    }
}