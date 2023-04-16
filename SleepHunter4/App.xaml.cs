using System.Windows;
using System.Windows.Threading;
using SleepHunter.Services;
using SleepHunter.Services.Logging;
using SleepHunter.Services.Releases;
using SleepHunter.Views;

namespace SleepHunter
{
    public partial class App : Application
    {
        public const string USER_MANUAL_URL = @"https://ewrogers.github.io/SleepHunter4/";

        private ILogger logger;

        public static new App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices();
            InitializeComponent();

            Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (logger == null)
                logger = Services.GetService<ILogger>();

            logger.LogError("Unhandled exception!");
            logger.LogException(e.Exception);

            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services.Dispose();
            base.OnExit(e);
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // Services
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IReleaseService, ReleaseService>();

            // ViewModels

            return services.BuildServiceProvider();
        }
    }
}
