using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SleepHunter.Updater
{
    internal partial class Obsidian
    {
        protected virtual void WindowThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb thumb))
                return;

            if (!(thumb.TemplatedParent is Window window))
                return;

            window.Left += e.HorizontalChange;
            window.Top += e.VerticalChange;
        }

        protected virtual void WindowThumb_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!(sender is Thumb thumb))
                return;

            if (!(thumb.TemplatedParent is Window window))
                return;

            if (window.ResizeMode == ResizeMode.NoResize)
                return;

            if (e.LeftButton.HasFlag(MouseButtonState.Pressed))
            {
                if (window.WindowState == WindowState.Maximized)
                    window.WindowState = WindowState.Normal;
                else
                    window.WindowState = WindowState.Maximized;
            }
        }

        protected virtual void WindowMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            if (!(button.TemplatedParent is Window window))
                return;

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Minimized;
        }

        protected virtual void WindowMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            if (!(button.TemplatedParent is Window window))
                return;

            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Maximized;
        }

        protected virtual void WindowClose_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            if (!(button.TemplatedParent is Window window))
                return;

            window.Close();
        }

        protected virtual void TextBoxClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button))
                return;

            if (!(button.TemplatedParent is TextBox textBox))
                return;

            textBox.Clear();
            textBox.Focus();
        }
    }
}
