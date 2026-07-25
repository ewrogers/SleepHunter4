using System;
using System.Windows;
using SleepHunter.Extensions;

namespace SleepHunter.Services.Clients
{
    public sealed class WpfClientLaunchInteraction :
        IClientLaunchInteraction
    {
        private readonly Window owner;

        public WpfClientLaunchInteraction(Window owner)
        {
            this.owner = owner ??
                throw new ArgumentNullException(nameof(owner));
        }

        public void ShowError(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            owner.ShowMessageBox(
                "Launch Client Failed",
                exception.Message,
                "Check that the executable and configured client layout are correct.",
                MessageBoxButton.OK,
                440,
                280);
        }
    }
}
