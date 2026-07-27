using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SleepHunter.Controls;
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
            InitializeComponent();
            BindTemplateResources();

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

            try
            {
                var mainWindow =
                    serviceProvider.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception exception)
            {
                if (logger == null)
                    logger = serviceProvider.GetRequiredService<ILogger>();

                logger.LogError("Application startup failed.");
                logger.LogException(exception);

                MessageBox.Show(
                    $"SleepHunter could not start.\n\n" +
                    exception.GetBaseException().Message,
                    "SleepHunter Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider.Dispose();
            base.OnExit(e);
        }

        private void BindTemplateResources()
        {
            var settingsManager =
                serviceProvider.GetRequiredService<UserSettingsManager>();
            BindTemplateResources(Resources, settingsManager);
        }

        internal static void BindTemplateResources(
            ResourceDictionary resources,
            UserSettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(resources);
            ArgumentNullException.ThrowIfNull(settingsManager);

            foreach (var dictionary in resources.MergedDictionaries)
            {
                if (!dictionary.Contains("UserSettingsManagerProxy"))
                    continue;

                if (dictionary["UserSettingsManagerProxy"]
                    is BindingProxy proxy)
                    proxy.Value = settingsManager;
            }
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
