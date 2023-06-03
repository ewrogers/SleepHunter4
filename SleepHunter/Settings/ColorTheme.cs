using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Media;

namespace SleepHunter.Settings
{
    [Serializable]
    public sealed class ColorTheme : ObservableObject
    {
        private const string DefaultBackgroundHex = "#1e3a8a";
        private const string DefaultForegroundHex = "#eff6ff";

        public static readonly ColorTheme DefaultTheme = new("Default", DefaultBackgroundHex, DefaultForegroundHex, 0, true);

        private string name;
        private int sortIndex;

        [NonSerialized]
        SolidColorBrush background;

        HueSaturationValue backgroundHsv;

        [NonSerialized]
        SolidColorBrush foreground;

        HueSaturationValue foregroundHsv;
        bool isDefault;

        [XmlAttribute("Name")]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        [XmlAttribute("SortIndex")]
        public int SortIndex
        {
            get => sortIndex;
            set => SetProperty(ref sortIndex, value);
        }

        [XmlIgnore]
        public SolidColorBrush Background
        {
            get => background;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SetProperty(ref background, value);
                SetProperty(ref backgroundHsv, new HueSaturationValue(value.Color), nameof(BackgroundHsv));
            }
        }

        [XmlAttribute("Background")]
        public string BackgroundHexColor
        {
            get => background.Color.ToString();
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var color = ColorConverter.ConvertFromString(value);

                if (color is Color bgColor)
                    Background = new SolidColorBrush(bgColor);
                else
                    throw new FormatException("Invalid hex color format.");
            }
        }

        [XmlIgnore]
        public byte BackgroundColorRed => background.Color.R;

        [XmlIgnore]
        public byte BackgroundColorGreen => background.Color.G;

        [XmlIgnore]
        public byte BackgroundColorBlue => background.Color.B;

        [XmlIgnore]
        public HueSaturationValue BackgroundHsv => backgroundHsv;

        [XmlIgnore]
        public HueSaturationValue ForegroundHsv => foregroundHsv;

        [XmlIgnore]
        public double BackgroundValue => Math.Max(Math.Max(background.Color.R, background.Color.G), background.Color.B);

        [XmlIgnore]
        public SolidColorBrush Foreground
        {
            get => foreground;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SetProperty(ref foreground, value);
                SetProperty(ref foregroundHsv, new HueSaturationValue(value.Color), nameof(ForegroundHsv));
            }
        }

        [XmlAttribute("Foreground")]
        public string ForegroundHexColor
        {
            get => foreground.Color.ToString();
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var color = ColorConverter.ConvertFromString(value);

                if (color is Color fgColor)
                    Foreground = new SolidColorBrush(fgColor);
                else
                    throw new FormatException("Invalid hex color format.");
            }
        }

        [XmlAttribute("IsDefault")]
        [DefaultValue(false)]
        public bool IsDefault
        {
            get => isDefault;
            set => SetProperty(ref isDefault, value);
        }

        private ColorTheme()
           : this(string.Empty, Brushes.Black, Brushes.White, 100, false) { }

        public ColorTheme(string key)
           : this(key, Brushes.Black, Brushes.White, 100, false) { }

        public ColorTheme(string name, string backgroundHexColor, string foregroundHexColor, int sortIndex, bool isDefault = false)
        {
            this.name = name;
            this.sortIndex = sortIndex;
            BackgroundHexColor = backgroundHexColor;
            ForegroundHexColor = foregroundHexColor;
            this.isDefault = isDefault;
        }

        public ColorTheme(string name, SolidColorBrush background, SolidColorBrush foreground, int sortIndex = 100, bool isDefault = false)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.sortIndex = sortIndex;
            this.background = background ?? throw new ArgumentNullException(nameof(background));
            this.foreground = foreground ?? throw new ArgumentNullException(nameof(foreground));
            this.isDefault = isDefault;
        }

        public override string ToString() => Name ?? string.Empty;
    }
}
