using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

using SleepHunter.Win32;

namespace SleepHunter.Themes
{
    internal partial class Obsidian
    {
        private const uint WindowNonClientLeftButtonDownMessage = 0x00A1;

        internal static nuint GetResizeHitTest(string resizeHandleName) =>
            resizeHandleName switch
            {
                "LeftResizeHandle" => 10,
                "RightResizeHandle" => 11,
                "TopResizeHandle" => 12,
                "TopLeftResizeHandle" => 13,
                "TopRightResizeHandle" => 14,
                "BottomResizeHandle" => 15,
                "BottomLeftResizeHandle" => 16,
                "BottomRightResizeHandle" => 17,
                _ => 0
            };

        protected virtual void WindowResizeHandle_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement resizeHandle)
                return;

            if (resizeHandle.TemplatedParent is not Window window)
                return;

            if (window.ResizeMode is not ResizeMode.CanResize
                and not ResizeMode.CanResizeWithGrip)
                return;

            if (window.WindowState != WindowState.Normal)
                return;

            var hitTest = GetResizeHitTest(resizeHandle.Name);
            if (hitTest == 0)
                return;

            var windowHandle = new WindowInteropHelper(window).Handle;
            if (windowHandle == nint.Zero)
                return;

            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(
                windowHandle,
                WindowNonClientLeftButtonDownMessage,
                hitTest,
                0);
            e.Handled = true;
        }

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
