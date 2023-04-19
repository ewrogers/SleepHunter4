using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SleepHunter.Updater.ViewModels;
using SleepHunter.Updater.Win32;

namespace SleepHunter.Updater
{
    public partial class App : Application
    {
        private static readonly string[] IgnoredFilenames = new string[] { "Updater.exe", "Settings.xml" };

        private MainWindowViewModel viewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Invalid number of arguments, exit
            // Usage: Updater.exe <zip file> <install path>
            if (e.Args.Length != 2)
            {
                Shutdown();
                return;
            }

            var updateFilePath = e.Args[0];
            var installationPath = e.Args[1];

            base.OnStartup(e);

            // Create the view model and main window
            viewModel = new MainWindowViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            mainWindow.Closing += MainWindow_Closing;

            // Listen for retry and cancel events
            viewModel.RetryRequested += async (sender, _) =>
            {
                ResetViewState();
                await Task.Delay(1000);

                ApplyUpdate(updateFilePath, installationPath);
            };

            viewModel.CancelRequested += (sender, _) =>
            {
                mainWindow.Close();
                Shutdown();
            };

            // Show the window and begin update
            mainWindow.Show();

            ResetViewState();
            ApplyUpdate(updateFilePath, installationPath);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Do not allow closing of the window while it is busy updating
            e.Cancel = viewModel.IsBusy;
        }

        private void ResetViewState()
        {
            viewModel.StatusText = "Preparing...";
            viewModel.CurrentFilename = string.Empty;
            viewModel.FileCountText = string.Empty;
            viewModel.ProgressPercent = 0;

            viewModel.ClearError();
        }

        private async void ApplyUpdate(string updateFilePath, string installationPath)
        {
            if (viewModel.IsBusy)
                return;

            viewModel.IsBusy = true;
            var executableName = Path.Combine(installationPath, "SleepHunter.exe");

            try
            {
                // Ensure the update file exists
                if (!File.Exists(updateFilePath))
                {
                    viewModel.SetError("Missing Update File", "You can try downloading it again, or installing the update manually.", false);
                    return;
                }

                try
                {
                    // Ensure the output directory exists
                    if (!Directory.Exists(installationPath))
                        Directory.CreateDirectory(installationPath);
                }
                catch
                {
                    viewModel.SetError("Invalid Installation Directory", $"The SleepHunter installation directory does not exist.\nYou should reinstall the application.", true);
                    return;
                }

                try
                {
                    // Close all running instances of the executable so it can be updated
                    viewModel.StatusText = "Waiting for SleepHunter...";
                    TerminateAndWait(executableName);
                }
                catch
                {
                    viewModel.SetError("SleepHunter Still Running", "Close all running instances of the application and try again.", true);
                    return;
                }

                try
                {
                    viewModel.StatusText = "Checking update file...";
                    using (var zipFile = ZipFile.OpenRead(updateFilePath))
                    {
                        // Do not extract files that should be ignored
                        var entries = from entry in zipFile.Entries
                                      where !IgnoredFilenames.Contains(entry.Name)
                                      select entry;

                        var totalCount = entries.Count();
                        var extractedCount = 0;

                        viewModel.StatusText = "Extracting files...";

                        foreach (var entry in entries)
                        {
                            viewModel.CurrentFilename = entry.Name;
                            viewModel.FileCountText = $"{extractedCount + 1} of {totalCount} files";

                            var outputFile = Path.Combine(installationPath, entry.FullName);
                            entry.ExtractToFile(outputFile, true);

                            extractedCount++;
                            viewModel.ProgressPercent = (extractedCount * 100) / totalCount;

                            await Task.Delay(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    viewModel.SetError("Unable to Extract Files", ex.Message, true);
                    return;
                }

                if (!File.Exists(executableName))
                {
                    viewModel.SetError("Missing SleepHunter Executable", "The SleepHunter executable could not be found.\nYou may have to reinstall the application.", true);
                    return;
                }

                try
                {
                    // Start SleepHunter process and bring to front
                    var process = Process.Start(executableName);
                    NativeMethods.SetForegroundWindow(process.MainWindowHandle);

                    Shutdown();
                }
                catch
                {
                    viewModel.SetError("Unable to Start SleepHunter", "You can restart it manually and check that the update succeeded.", false);
                    return;
                }
            }
            finally
            {
                viewModel.IsBusy = false;
            }
        }

        private static void TerminateAndWait(string executableFile, int timeoutSeconds = 10)
        {
            var processName = Path.GetFileNameWithoutExtension(executableFile);
            var processes = Enumerable.Empty<Process>();

            processes = from process in Process.GetProcessesByName(processName)
                        where process.MainModule.FileName == executableFile
                        select process;

            foreach (var process in processes)
            {
                process.Kill();

                if (!process.WaitForExit(timeoutSeconds * 1000))
                    throw new TimeoutException("Timeout elapsed with processes still running");
            }
        }
    }
}
