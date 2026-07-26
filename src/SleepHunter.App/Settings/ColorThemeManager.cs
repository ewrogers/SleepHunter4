using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SleepHunter.Settings
{
    public sealed class ColorThemeManager
    {
        public const string ThemesFile = @"Themes.xml";

        private static readonly ColorThemeManager instance = new();
        public static ColorThemeManager Instance => instance;

        private ColorThemeManager() { }

        private readonly ConcurrentDictionary<string, ColorTheme> colorThemes = new(StringComparer.OrdinalIgnoreCase);

        public ColorTheme this[string key]
        {
            get => GetTheme(key);
            set => AddTheme(value);
        }

        public int Count => colorThemes.Count;

        public IEnumerable<ColorTheme> Themes => colorThemes.Values.OrderBy(theme => theme.SortIndex);

        public ColorTheme DefaultTheme => Themes.FirstOrDefault(theme => theme.IsDefault) ?? ColorTheme.DefaultTheme;

        public void AddTheme(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            if (string.IsNullOrWhiteSpace(theme.Name))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(theme.Name));

            colorThemes[theme.Name] = theme;
        }

        public ColorTheme GetTheme(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return colorThemes[key];
        }

        public bool ContainsTheme(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return colorThemes.ContainsKey(key);
        }

        public void LoadFromFile(string filename)
        {
            using var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            LoadFromStream(inputStream);
        }

        public void LoadFromStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(ColorThemeCollection));

            if (serializer.Deserialize(stream) is not ColorThemeCollection collection)
                return;

            foreach (var theme in collection.Themes)
                AddTheme(theme);
        }

        public void SaveToFile(string filename)
        {
            using var outputStream = File.Create(filename);
            SaveToStream(outputStream);
            outputStream.Flush();
        }

        public void SaveToStream(Stream stream)
        {
            var collection = new ColorThemeCollection(Themes);
            var serializer = new XmlSerializer(typeof(ColorThemeCollection));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            serializer.Serialize(stream, collection, namespaces);
        }

        public void ApplyTheme(string themeKey)
        {
            if (themeKey == null)
                return;

            if (!colorThemes.ContainsKey(themeKey))
                return;

            ApplyThemeResources(colorThemes[themeKey]);
        }

        public void ApplyDefaultTheme()
        {
            ApplyThemeResources(DefaultTheme);
        }

        internal static Color CreateAccentInsetColor(
            Color accentColor)
        {
            const byte insetAlpha = 0x20;
            var channel = GetContrastingChannel(accentColor);

            return Color.FromArgb(
                insetAlpha,
                channel,
                channel,
                channel);
        }

        internal static Color CreateAccentForegroundColor(
            Color accentColor)
        {
            var channel = GetContrastingChannel(accentColor);
            return Color.FromArgb(
                byte.MaxValue,
                channel,
                channel,
                channel);
        }

        private static byte GetContrastingChannel(
            Color accentColor)
        {
            var luminance =
                0.2126 * ToLinearColorValue(accentColor.R) +
                0.7152 * ToLinearColorValue(accentColor.G) +
                0.0722 * ToLinearColorValue(accentColor.B);
            var blackContrast = (luminance + 0.05) / 0.05;
            var whiteContrast = 1.05 / (luminance + 0.05);
            return blackContrast >= whiteContrast
                ? (byte)0
                : (byte)255;
        }

        private static void ApplyThemeResources(ColorTheme theme)
        {
            var resources = Application.Current.Resources;
            resources["ObsidianBackground"] = theme.Background;
            resources["ObsidianForeground"] = theme.Foreground;
            resources["ObsidianAccentInsetBorderBrush"] =
                new SolidColorBrush(
                    CreateAccentInsetColor(theme.Background.Color));
            resources["ObsidianAccentForeground"] =
                new SolidColorBrush(
                    CreateAccentForegroundColor(
                        theme.Background.Color));
        }

        private static double ToLinearColorValue(byte channel)
        {
            var value = channel / 255.0;
            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
