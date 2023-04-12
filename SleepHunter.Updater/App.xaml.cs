using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using SleepHunter.Updater.ViewModels;

namespace SleepHunter.Updater
{
    public partial class App : Application
    {
        private static readonly string[] IgnoredFilenames = new string[] { "Updater.exe", "Settings.xml" };

        private MainWindowViewModel viewModel;

        protected override async void OnStartup(StartupEventArgs e)
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

            // Attach to closing event and show window
            mainWindow.Closing += MainWindow_Closing;
            mainWindow.Show();

            // Start the update
            while (true)
            {
                await Task.Delay(5000);
                viewModel.SetError("File Not Found", "The update file was not found.\nTry downloading it again.");

                await Task.Delay(5000);
                viewModel.ClearError();
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Do not allow closing of the window while it is busy updating
            e.Cancel = viewModel.IsBusy;
        }
    }
}
