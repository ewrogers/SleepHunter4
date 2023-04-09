using System;

namespace SleepHunter.Settings
{
    public delegate void ColorThemeEventHandler(object sender, ColorThemeEventArgs e);

    public sealed class ColorThemeEventArgs : EventArgs
    {
        readonly ColorTheme theme;

        public ColorTheme Theme { get { return theme; } }

        public ColorThemeEventArgs(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException("theme");

            this.theme = theme;
        }
    }
}
