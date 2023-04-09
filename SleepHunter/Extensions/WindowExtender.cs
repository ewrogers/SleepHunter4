using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using SleepHunter.Views;

namespace SleepHunter.Extensions
{
    public static class WindowExtender
    {
        public static T InvokeIfRequired<T>(this Dispatcher dispatcher, Func<T> action, T value, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (dispatcher.Thread != Thread.CurrentThread)
                return (T)dispatcher.Invoke(action, priority, null);
            else
                return action();
        }

        public static void InvokeIfRequired(this Dispatcher dispatcher, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (dispatcher.Thread != Thread.CurrentThread)
                dispatcher.Invoke(action, priority, null);
            else
                action();
        }

        public static void InvokeIfRequired<T>(this Dispatcher dispatcher, Action<T> action, T value, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (dispatcher.Thread != Thread.CurrentThread)
                dispatcher.Invoke(action, priority, value);
            else
                action(value);
        }

        public static bool? ShowMessageBox(this Window owner, string windowTitle, string messageText, string subText = null, MessageBoxButton buttons = MessageBoxButton.OK, int width = 420, int height = 260)
        {
            var messageBox = new MessageBoxWindow();
            messageBox.Title = windowTitle ?? string.Empty;
            messageBox.Width = width;
            messageBox.Height = height;
            messageBox.MessageText = messageText ?? string.Empty;
            messageBox.SubText = subText ?? string.Empty;

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
