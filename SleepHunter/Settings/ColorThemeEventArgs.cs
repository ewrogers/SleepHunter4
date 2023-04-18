using System;

namespace SleepHunter.Settings
{
    public delegate void ColorThemeEventHandler(object sender, ColorThemeEventArgs e);

    public sealed class ColorThemeEventArgs : EventArgs
    {
        public ColorTheme Theme { get; }

        public ColorThemeEventArgs(ColorTheme theme)
            => Theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }
}
