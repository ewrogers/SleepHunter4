using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Services.Clients;
using SleepHunter.Services.Logging;
using SleepHunter.Settings;

namespace SleepHunter.ViewModels
{
    public sealed partial class ClientLaunchViewModel :
        ObservableObject
    {
        private readonly Func<ClientLayout> getLayout;
        private readonly Func<UserSettings> getSettings;
        private readonly IClientLaunchInteraction interaction;
        private readonly IClientLaunchService launcher;
        private readonly ILogger logger;

        public ClientLaunchViewModel(
            IClientLaunchService launcher,
            IClientLaunchInteraction interaction,
            Func<UserSettings> getSettings,
            Func<ClientLayout> getLayout,
            ILogger logger)
        {
            this.launcher = launcher ??
                throw new ArgumentNullException(nameof(launcher));
            this.interaction = interaction ??
                throw new ArgumentNullException(
                    nameof(interaction));
            this.getSettings = getSettings ??
                throw new ArgumentNullException(
                    nameof(getSettings));
            this.getLayout = getLayout ??
                throw new ArgumentNullException(
                    nameof(getLayout));
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(
            nameof(LaunchClientCommand))]
        public partial bool IsLayoutAvailable
        {
            get;
            set;
        }

        [RelayCommand(
            CanExecute = nameof(CanLaunchClient))]
        private void LaunchClient()
        {
            var layout = getLayout();
            if (layout is null)
                return;

            try
            {
                launcher.Launch(
                    new ClientLaunchOptions(
                        getSettings()),
                    layout);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Unable to launch a new client");
                logger.LogException(exception);
                interaction.ShowError(exception);
            }
        }

        private bool CanLaunchClient() =>
            IsLayoutAvailable;
    }
}
