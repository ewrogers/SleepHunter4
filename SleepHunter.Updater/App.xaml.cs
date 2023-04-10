using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SleepHunter.Updater
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();

            var updateFilePath = Path.Combine(Path.GetTempPath(), "SleepHunter-4.0.1.zip");
            var installationPath = @"C:\SleepHunter";

            if (e.Args.Length > 0) updateFilePath = e.Args[0];
            if (e.Args.Length > 1) installationPath = e.Args[1];

            mainWindow.Show();

            mainWindow.PerformAppUpdate(updateFilePath, installationPath);

            var execPath = Path.Combine(installationPath, "SleepHunter.exe");
            if (!File.Exists(execPath))
            {
                mainWindow.SetStatusText("Unable to Restart");
                mainWindow.SetErrorMessage("Missing SleepHunter executable in installation folder.\nYou may need to apply the update manually.");
                return;
            }

            mainWindow.SetStatusText("Restarting SleepHunter...");
            Process.Start(execPath);

            Shutdown();
        }
    }
}
