using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace SleepHunter.Updater
{
    public partial class MainWindow : Window
    {
        private bool isUpdating;

        public MainWindow()
        {
            InitializeComponent();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetStatusText("Opening update file...");
            SetProgress(0);
            SetErrorMessage(string.Empty);

            progressFileText.Text = string.Empty;
            progressCountText.Text = string.Empty;

            GetVersion();
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Disallow closing the window while updating
            e.Cancel = isUpdating;
        }

        void GetVersion()
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
        public void SetErrorMessage(string message) => errorMessageText.Text = message;

        public void PerformAppUpdate(string updateFilePath, string destinationFolder)
        {
            isUpdating = true;

            try
            {
                if (!File.Exists(updateFilePath))
                    throw new FileNotFoundException("Missing update file", updateFilePath);

                var extractedCount = 0;

                using (var archive = ZipFile.OpenRead(updateFilePath))
                {
                    var entries = from entry in archive.Entries
                                  where entry.Name != "Updater.exe"
                                  select entry;

                    var entryCount = entries.Count();

                    SetStatusText("Extracting files...");
                    SetProgress(0);

                    foreach (var fileEntry in entries)
                    {
                        SetProgressFile(fileEntry.Name);
                        SetProgressCount(extractedCount + 1, entryCount);

                        var outputFile = Path.Combine(destinationFolder, fileEntry.Name);
                        fileEntry.ExtractToFile(outputFile, true);

                        extractedCount++;
                        var percentCompleted = (extractedCount * 100) / entryCount;
                        SetProgress(percentCompleted);
                    }
                }

                SetStatusText("Update Completed");
                SetProgress(100);
            }
            catch (Exception ex)
            {
                SetErrorMessage(ex.Message);
                throw ex;
            }
            finally { isUpdating = false; }
        }
    }
}
