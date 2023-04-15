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
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        [XmlAttribute("Category")]
        public int SortIndex
        {
            get { return sortIndex; }
            set { SetProperty(ref sortIndex, value); }
        }

        [XmlIgnore]
        public SolidColorBrush Background
        {
            get { return background; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                SetProperty(ref background, value);
                SetProperty(ref backgroundHsv, new HueSaturationValue(value.Color), "BackgroundHsv");
            }
        }

        [XmlAttribute("Background")]
        public string BackgroundHexColor
        {
            get { return background.Color.ToString(); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                var color = ColorConverter.ConvertFromString(value);

                if (color is Color)
                    Background = new SolidColorBrush((Color)color);
                else
                    throw new FormatException("Invalid hex color format.");
            }
        }

        [XmlIgnore]
        public byte BackgroundColorRed
        {
            get { return background.Color.R; }
        }

        [XmlIgnore]
        public byte BackgroundColorGreen
        {
            get { return background.Color.G; }
        }

        [XmlIgnore]
        public byte BackgroundColorBlue
        {
            get { return background.Color.B; }
        }

        [XmlIgnore]
        public HueSaturationValue BackgroundHsv
        {
            get { return backgroundHsv; }
        }

        [XmlIgnore]
        public HueSaturationValue ForegroundHsv
        {
            get { return foregroundHsv; }
        }

        [XmlIgnore]
        public double BackgroundValue
        {
            get { return Math.Max(Math.Max(background.Color.R, background.Color.G), background.Color.B); }
        }

        [XmlIgnore]
        public SolidColorBrush Foreground
        {
            get { return foreground; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                SetProperty(ref foreground, value);
                SetProperty(ref foregroundHsv, new HueSaturationValue(value.Color), "ForegroundHsv");
            }
        }

        [XmlAttribute("Foreground")]
        public string ForegroundHexColor
        {
            get { return foreground.Color.ToString(); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                var colorValue = ColorConverter.ConvertFromString(value);

                if (colorValue is Color color)
                    Foreground = new SolidColorBrush(color);
                else
                    throw new FormatException("Invalid hex color format.");
            }
        }

        [XmlAttribute("IsDefault")]
        [DefaultValue(false)]
        public bool IsDefault
        {
            get { return isDefault; }
            set { SetProperty(ref isDefault, value); }
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
            this.name = name ?? throw new ArgumentNullException("name");
            this.sortIndex = sortIndex;
            this.background = background ?? throw new ArgumentNullException("background");
            this.foreground = foreground ?? throw new ArgumentNullException("foreground");
            this.isDefault = isDefault;
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }
}
