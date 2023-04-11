using System.Windows;
using SleepHunter.Services;
using SleepHunter.Services.Logging;
using SleepHunter.Services.Releases;
using SleepHunter.Views;

namespace SleepHunter
{
    public partial class App : Application
    {
        public static new App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // Services
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IReleaseService, ReleaseService>();
            
            // ViewModels

            return services;
        }
    }
}
