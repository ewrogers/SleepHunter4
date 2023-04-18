using System;

namespace SleepHunter.Settings
{
    internal delegate void ColorThemeEventHandler(object sender, ColorThemeEventArgs e);

    internal sealed class ColorThemeEventArgs : EventArgs
    {
        private readonly ColorTheme theme;

        public ColorTheme Theme => theme;

        public ColorThemeEventArgs(ColorTheme theme)
        {
            this.theme = theme ?? throw new ArgumentNullException(nameof(theme));
        }
    }
}
