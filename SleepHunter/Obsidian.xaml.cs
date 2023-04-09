using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SleepHunter.Themes
{
    internal partial class Obsidian
    {
        protected virtual void WindowThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            if (thumb == null)
                return;

            var window = thumb.TemplatedParent as Window;
            if (window == null)
                return;

            window.Left += e.HorizontalChange;
            window.Top += e.VerticalChange;
        }

        protected virtual void WindowThumb_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var thumb = sender as Thumb;
            if (thumb == null)
                return;

            var window = thumb.TemplatedParent as Window;
            if (window == null)
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
            var button = sender as Button;
            if (button == null)
                return;

            var window = button.TemplatedParent as Window;
            if (window == null)
                return;

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Minimized;
        }

        protected virtual void WindowMaximize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var window = button.TemplatedParent as Window;
            if (window == null)
                return;

            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Maximized;
        }

        protected virtual void WindowClose_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var window = button.TemplatedParent as Window;
            if (window == null)
                return;

            window.Close();
        }

        protected virtual void TextBoxClearButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var textBox = button.TemplatedParent as TextBox;
            if (textBox == null)
                return;

            textBox.Clear();
            textBox.Focus();
        }
    }
}
