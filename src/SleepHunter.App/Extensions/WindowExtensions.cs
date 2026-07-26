using System;
using SleepHunter.Views;
using System.Windows;

namespace SleepHunter.Extensions
{
    public static class WindowExtensions
    {
        public static bool? ShowMessageBox(this Window owner, string windowTitle, string messageText, string subText = null, MessageBoxButton buttons = MessageBoxButton.OK, int width = 420, int height = 280)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            var messageBox = new MessageBoxWindow
            {
                Title = windowTitle ?? string.Empty,
                Width = width,
                Height = height,
                MessageText = messageText ?? string.Empty,
                SubText = subText ?? string.Empty
            };

            if (buttons == MessageBoxButton.OK)
            {
                messageBox.CancelButtonColumnWidth = new GridLength(1, GridUnitType.Auto);
                messageBox.CancelButtonVisibility = Visibility.Collapsed;
            }

            if (buttons.HasFlag(MessageBoxButton.YesNo))
            {
                messageBox.OkButtonText = "_Yes";
                messageBox.CancelButtonText = "_No";
            }

            if (!owner.IsLoaded)
                owner.Show();

            messageBox.Owner = owner;

            return messageBox.ShowDialog();
        }
    }
}
