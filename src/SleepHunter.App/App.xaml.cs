using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SleepHunter.IO;
using SleepHunter.Media;
using SleepHunter.Metadata;
using SleepHunter.Services.Clients;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Hotkeys;
using SleepHunter.Services.Logging;
using SleepHunter.Services.Releases;
using SleepHunter.Services.Runtime;
using SleepHunter.Settings;
using SleepHunter.Views;

namespace SleepHunter
{
    public partial class App : Application
    {
        public const string USER_MANUAL_URL = @"https://ewrogers.github.io/SleepHunter4/";

        private readonly ServiceProvider serviceProvider;
        private ILogger logger;

        public App()
        {
            serviceProvider = ConfigureServices();
            Resources["ColorThemeManager"] =
                serviceProvider.GetRequiredService<ColorThemeManager>();
            Resources["SkillMetadataManager"] =
                serviceProvider.GetRequiredService<SkillMetadataManager>();
            Resources["SpellMetadataManager"] =
                serviceProvider.GetRequiredService<SpellMetadataManager>();
            Resources["StaffMetadataManager"] =
                serviceProvider.GetRequiredService<StaffMetadataManager>();
            Resources["UserSettingsManager"] =
                serviceProvider.GetRequiredService<UserSettingsManager>();
            InitializeComponent();
            Resources["ColorThemeManager"] =
                serviceProvider.GetRequiredService<ColorThemeManager>();
            Resources["SkillMetadataManager"] =
                serviceProvider.GetRequiredService<SkillMetadataManager>();
            Resources["SpellMetadataManager"] =
                serviceProvider.GetRequiredService<SpellMetadataManager>();
            Resources["StaffMetadataManager"] =
                serviceProvider.GetRequiredService<StaffMetadataManager>();
            Resources["UserSettingsManager"] =
                serviceProvider.GetRequiredService<UserSettingsManager>();

            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (logger == null)
                logger = serviceProvider.GetRequiredService<ILogger>();

            logger.LogError("Unhandled exception!");
            logger.LogException(e.Exception);

            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppContext.BaseDirectory);
            base.OnStartup(e);

            var mainWindow =
                serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider.Dispose();
            base.OnExit(e);
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<ClientLayoutManager>();
            services.AddSingleton<ColorThemeManager>();
            services.AddSingleton<UserSettingsManager>();
            services.AddSingleton<FileArchiveManager>();
            services.AddSingleton<IconManager>();
            services.AddSingleton<SkillMetadataManager>();
            services.AddSingleton<SpellMetadataManager>();
            services.AddSingleton<StaffMetadataManager>();
            services.AddSingleton<IReleaseService, ReleaseService>();
            services.AddSingleton<
                IClientLaunchService,
                ClientLaunchService>();
            services.AddSingleton<
                IClientProcessScanner,
                WindowsClientProcessScanner>();
            services.AddSingleton<ClientSessionRegistry>();
            services.AddSingleton<WindowHotkeyRegistry>();
            services.AddSingleton<ClientMacroConfigurationRegistry>();
            services.AddSingleton<
                IMacroConfigurationReader,
                FileMacroConfigurationReader>();
            services.AddSingleton<
                IMacroConfigurationWriter,
                FileMacroConfigurationWriter>();
            services.AddSingleton<
                IClientMacroConfigurationMapper,
                ClientMacroConfigurationMapper>();
            services.AddSingleton<
                IRuntimeStaffCandidateProvider,
                RuntimeStaffCandidateProvider>();
            services.AddSingleton<
                IRuntimeAutomationSetupFactory,
                RuntimeAutomationSetupFactory>();

            // ViewModels
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
        }
    }
}
