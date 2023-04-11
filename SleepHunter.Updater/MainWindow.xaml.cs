using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;
using SleepHunter.Settings;

namespace SleepHunter.Updater
{
    public partial class MainWindow : Window
    {
        private static readonly string[] IgnoredFilenames = new string[] { "Updater.exe", "Settings.xml" };
        private bool isUpdating;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            LoadThemes();
            ApplyTheme();

            SetStatusText("Opening update file...");
            SetProgress(0);
            SetErrorMessage(string.Empty);

            progressFileText.Text = string.Empty;
            progressCountText.Text = string.Empty;

            GetVersion();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Disallow closing the window while updating
            e.Cancel = isUpdating;
        }

        private void LoadSettings()
        {
            var settingsFile = UserSettingsManager.SettingsFile;
            if (File.Exists(settingsFile))
            {
                try
                {
                    UserSettingsManager.Instance.LoadFromFile(settingsFile);
                }
                catch
                {
                    UserSettingsManager.Instance.Settings.ResetDefaults();
                }
            }
            else UserSettingsManager.Instance.Settings.ResetDefaults();
        }

        private void LoadThemes()
        {
            var themesFile = ColorThemeManager.ThemesFile;
            if (File.Exists(themesFile))
            {
                try
                {
                    ColorThemeManager.Instance.LoadFromFile(themesFile);
                }
                catch
                {
                    ColorThemeManager.Instance.LoadDefaultThemes();
                }
            }
            else ColorThemeManager.Instance.LoadDefaultThemes();
        }

        private void ApplyTheme()
        {
            if (!UserSettingsManager.Instance.Settings.RainbowMode)
            {
                var themeName = UserSettingsManager.Instance.Settings.SelectedTheme;
                ColorThemeManager.Instance.ApplyTheme(themeName);
            }
            else
            {
                ColorThemeManager.Instance.ApplyRainbowMode();
            }
        }

        private void GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var isDebug = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration == "Debug";

            versionText.Text = $"Updater - v{version.Major}.{version.Minor}.{version.Build}";

            if (isDebug)
                versionText.Text += "  (Debug)";
        }

        public void SetStatusText(string message) => statusText.Text = message;

        public void SetProgress(int progress)
        {
            progressBar.Value = progress;
            progressPercentText.Text = $"{progress}%";
        }

        public void SetProgressFile(string filename) => progressFileText.Text = filename;

        public void SetProgressCount(int current, int max) => progressCountText.Text = $"{current} of {max} files";
        public void SetErrorMessage(string message)
        {
            warningIcon.Visibility = !string.IsNullOrWhiteSpace(message) ? Visibility.Visible : Visibility.Collapsed;
            errorMessageText.Text = message;
        }

        public void PerformAppUpdate(string updateFilePath, string destinationFolder)
        {
            isUpdating = true;

            try
            {
                if (!File.Exists(updateFilePath))
                    throw new FileNotFoundException("Missing update file", updateFilePath);

                if (!Directory.Exists(destinationFolder))
                    Directory.CreateDirectory(destinationFolder);

                var extractedCount = 0;

                using (var archive = ZipFile.OpenRead(updateFilePath))
                {
                    var entries = from entry in archive.Entries
                                  where !IgnoredFilenames.Contains(entry.Name)
                                  select entry;

                    var entryCount = entries.Count();

                    SetStatusText("Extracting files...");
                    SetProgress(0);

                    foreach (var fileEntry in entries)
                    {
                        SetProgressFile(fileEntry.Name);
                        SetProgressCount(extractedCount + 1, entryCount);

                        var outputFile = Path.Combine(destinationFolder, fileEntry.Name);

                        // Do not overwrite a user's Settings.xml file!
                        var overwrite = fileEntry.Name != "Settings.xml";

                        fileEntry.ExtractToFile(outputFile, true);

                        extractedCount++;
                        var percentCompleted = (extractedCount * 100) / entryCount;
                        SetProgress(percentCompleted);
                    }
                }

                SetStatusText("Update Completed");
                SetProgress(100);
            }
            finally { isUpdating = false; }
        }
    }
}
