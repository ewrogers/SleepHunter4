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

            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();

        }
    }
}
