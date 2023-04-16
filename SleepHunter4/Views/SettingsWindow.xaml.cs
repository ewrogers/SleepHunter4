using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SleepHunter.Extensions;
using SleepHunter.Models;
using SleepHunter.Services.Logging;
using SleepHunter.Services.Releases;
using SleepHunter.Settings;

namespace SleepHunter.Views
{
    public partial class SettingsWindow : Window
    {
        public static readonly int GeneralTabIndex = 0;
        public static readonly int UserInterfaceTabIndex = 1;
        public static readonly int GameClientTabIndex = 2;
        public static readonly int AllMacrosTabIndex = 3;
        public static readonly int SkillMacrosTabIndex = 4;
        public static readonly int SpellMacrosTabIndex = 5;
        public static readonly int FloweringTabIndex = 6;
        public static readonly int UpdatesTabIndex = 7;
        public static readonly int DebugTabIndex = 8;
        public static readonly int AboutTabIndex = 9;

        private readonly ILogger logger;
        private readonly IReleaseService releaseService;

        private Version currentVersion;
        private ReleaseVersion latestRelease;
        private bool isCheckingForVersion;

        public int SelectedTabIndex
        {
            get { return (int)GetValue(SelectedTabIndexProperty); }
            set { SetValue(SelectedTabIndexProperty, value); }
        }

        public static readonly DependencyProperty SelectedTabIndexProperty =
            DependencyProperty.Register("SelectedTabIndex", typeof(int), typeof(SettingsWindow), new PropertyMetadata(0));

        public SettingsWindow()
        {
            logger = App.Current.Services.GetService<ILogger>();
            releaseService = App.Current.Services.GetService<IReleaseService>();

            InitializeComponent();
            // ToDo: Fix Assembly Get logic to pull in correct values
            //GetVersion();
            ToggleDownloadUpdateButton(false);
        }

        void GetVersion()
        {
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var isDebug = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration == "Debug";
            versionText.Text = $"Version {currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";

            var buildNumber = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var buildYear = Convert.ToInt32(buildNumber.Substring(0, 2)) + 2000;
            var buildMonth = Convert.ToInt32(buildNumber.Substring(2, 1), 16);
            var buildDay = Convert.ToInt32(buildNumber.Substring(3, 2));

            var buildDate = new DateTime(buildYear, buildMonth, buildDay);

            buildText.Text = $"Build {buildNumber}";
            buildDateText.Text = $"{buildDate:MMMM} {buildDate.Day}{GetDayOrdinal(buildDate.Day)} {buildDate:yyyy}";

            currentVersionText.Text = $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";

            if (isDebug)
                buildText.Text += "  (Debug)";
        }

        async Task CheckForLatestVersion()
        {
            if (isCheckingForVersion)
                return;

            ToggleDownloadUpdateButton(false);

            isCheckingForVersion = true;
            updateAvailableText.Text = "Checking for updates...";

            try
            {
                checkForUpdateButton.IsEnabled = false;
                latestVersionPlaceholderText.Visibility = Visibility.Visible;
                latestVersionText.Visibility = Visibility.Collapsed;

                latestRelease = await releaseService.GetLatestReleaseVersionAsync();
                var version = latestRelease.Version;

                latestVersionText.Text = $"{version.Major}.{version.Minor}.{version.Build}";

                latestVersionPlaceholderText.Visibility = Visibility.Collapsed;
                latestVersionText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                latestVersionText.Text = string.Empty;
                latestVersionPlaceholderText.Text = "Unknown";

                latestVersionPlaceholderText.Visibility = Visibility.Visible;
                latestVersionText.Visibility = Visibility.Collapsed;

                this.ShowMessageBox("Network Error", "Unable to check for latest version:", ex.Message, MessageBoxButton.OK);
            }
            finally
            {
                checkForUpdateButton.IsEnabled = true;
                isCheckingForVersion = false;

                var isUpdateAvaialble = latestRelease != null && latestRelease.Version.IsNewerThan(currentVersion);
                updateAvailableText.Text = isUpdateAvaialble ? "There is an update available." : "You have the latest version.";

                ToggleDownloadUpdateButton(isUpdateAvaialble);
            }
        }

        static string GetDayOrdinal(int dayOfMonth)
        {
            if (dayOfMonth <= 0)
                return string.Empty;

            switch (dayOfMonth)
            {
                case 11:
                case 12:
                case 13:
                    return "th";
            }

            switch (dayOfMonth % 10)
            {
                case 1:
                    return "st";
                case 2:
                    return "nd";
                case 3:
                    return "rd";
                default:
                    return "th";
            }
        }

        void ToggleDownloadUpdateButton(bool showHide)
        {
            downloadUpdateButton.IsEnabled = showHide;
            downloadUpdateButton.Visibility = showHide ? Visibility.Visible : Visibility.Collapsed;
        }

        void resetDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            bool? isOkToReset = this.ShowMessageBox("Reset Default Settings",
               "This will reset all settings to their default values.\nDo you wish to continue?",
               "This action cannot be undone.",
               MessageBoxButton.YesNo,
               460, 240);

            if (isOkToReset.Value)
                UserSettingsManager.Instance.Settings.ResetDefaults();
        }

        async void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is TabControl tabControl))
                return;

            if (!(tabControl.SelectedItem is TabItem tabItem))
            {
                Title = "Settings";
                return;
            }

            var headerName = (tabItem.Header as string).Replace("_", string.Empty);
            logger.LogInfo($"User has selected '{headerName}' tab in Settings window");

            Title = $"Settings - {headerName}";

            if (tabItem.TabIndex == UpdatesTabIndex)
            {
                if (latestRelease == null)
                    await CheckForLatestVersion();
            }
        }

        void userManualLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(App.USER_MANUAL_URL) { UseShellExecute = true });
        }

        async void checkForUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(Owner is MainWindow mainWindow))
                return;

            await CheckForLatestVersion();
        }

        void releaseNotesLink_Click(object sender, RoutedEventArgs e)
        {
            var uri = releaseService.GetLatestReleaseNotesUri();
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }

        void downloadUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(Owner is MainWindow mainWindow))
                return;

            downloadUpdateButton.IsEnabled = false;

            Close();
            mainWindow.DownloadAndInstallUpdate();
        }
    }
}
