using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SleepHunter.Updater
{
    public partial class App : Application
    {
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
            var executableFile = Path.Combine(installationPath, "SleepHunter.exe");

            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Check that the update file exists, show error if missing
            if (!File.Exists(updateFilePath))
            {
                mainWindow.SetStatusText("Unable to Update");
                mainWindow.SetErrorMessage("Missing update file.\nYou can try again within SleepHunter, or install it manually.");
                return;
            }

            // Terminate any existing SleepHunter instances
            try
            {
                mainWindow.SetStatusText("Preparing...");
                
                foreach (var process in Process.GetProcessesByName("SleepHunter"))
                    process.Kill();
            }
            catch (Exception ex)
            {
                mainWindow.SetStatusText("Unable to Update");
                mainWindow.SetErrorMessage(ex.Message);
                return;
            }

            // Try to update, and display an error if something goes wrong
            try
            {
                mainWindow.PerformAppUpdate(updateFilePath, installationPath);
            }
            catch (Exception ex)
            {
                mainWindow.SetStatusText("Unable to Update");
                mainWindow.SetErrorMessage(ex.Message);
                return;
            }

            // Check that the executable exists, show error if missing
            if (!File.Exists(executableFile))
            {
                mainWindow.SetStatusText("Unable to Restart");
                mainWindow.SetErrorMessage("Missing SleepHunter executable in installation folder.\nYou may need to apply the update manually.");
                return;
            }

            // Restart SleepHunter
            mainWindow.SetStatusText("Restarting SleepHunter...");
            Process.Start(executableFile);

            Shutdown();
        }
    }
}
