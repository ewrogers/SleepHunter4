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
        private SolidColorBrush background;

        private HueSaturationValue backgroundHsv;

        [NonSerialized]
        private SolidColorBrush foreground;

        private HueSaturationValue foregroundHsv;
        private bool isDefault;

        [XmlAttribute(nameof(Name))]
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
                OnPropertyChanged(nameof(BackgroundValue));
                OnPropertyChanged(nameof(BackgroundHexColor));
                OnPropertyChanged(nameof(BackgroundColorRed));
                OnPropertyChanged(nameof(BackgroundColorGreen));
                OnPropertyChanged(nameof(BackgroundColorBlue));
            }
        }

        [XmlAttribute("Background")]
        public string BackgroundHexColor
        {
            get => $"{background.Color}";
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var colorValue = ColorConverter.ConvertFromString(value);

                if (colorValue is Color color)
                    Background = new SolidColorBrush(color);
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
        public double BackgroundValue => Math.Max(Math.Max(background.Color.R, background.Color.G), background.Color.B);

        [XmlIgnore]
        public byte ForegroundColorRed => foreground.Color.R;

        [XmlIgnore]
        public byte ForegroundColorGreen => foreground.Color.G;

        [XmlIgnore]
        public byte ForegroundColorBlue => foreground.Color.B;

        [XmlIgnore]
        public HueSaturationValue ForegroundHsv => foregroundHsv;

        [XmlIgnore]
        public double ForegroundValue => Math.Max(Math.Max(foreground.Color.R, foreground.Color.G), foreground.Color.B);

        [XmlIgnore]
        public SolidColorBrush Foreground
        {
            get => foreground;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SetProperty(ref foreground, value);
                SetProperty(ref foregroundHsv, new HueSaturationValue(value.Color), "ForegroundHsv");
                OnPropertyChanged(nameof(ForegroundValue));
                OnPropertyChanged(nameof(ForegroundColorRed));
                OnPropertyChanged(nameof(ForegroundColorBlue));
                OnPropertyChanged(nameof(ForegroundColorGreen));
            }
        }

        [XmlAttribute("Foreground")]
        public string ForegroundHexColor
        {
            get => $"foreground.Color";
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var colorValue = ColorConverter.ConvertFromString(value);

                if (colorValue is Color color)
                    Foreground = new SolidColorBrush(color);
                else
                    throw new FormatException("Invalid hex color format.");
            }
        }

        [XmlAttribute(nameof(IsDefault))]
        [DefaultValue(false)]
        public bool IsDefault
        {
            get => isDefault;
            set => SetProperty(ref isDefault, value);
        }

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

        public override string ToString() => Name ?? "Unnamed Theme";
    }
}
