using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace SleepHunter.Settings
{
    public sealed class ColorThemeManager
    {
        public static readonly string ThemesFile = @"Themes.xml";

        static readonly string WhiteHexString = @"#FFFFFF";

        #region Default Themes
        static readonly List<ColorTheme> DefaultThemes = new List<ColorTheme>();
        #endregion

        #region Singleton
        static readonly ColorThemeManager instance = new ColorThemeManager();

        public static ColorThemeManager Instance { get { return instance; } }

        private ColorThemeManager()
        {
            // Google Material Color Palette
            DefaultThemes.Add(new ColorTheme("Cherry", "#F44336", WhiteHexString)); // Red 500
            DefaultThemes.Add(new ColorTheme("Scarlet", "#D32F2F", WhiteHexString)); // Red 700
            DefaultThemes.Add(new ColorTheme("Crimson", "#B71C1C", WhiteHexString)); // Red 900

            DefaultThemes.Add(new ColorTheme("Sakura", "#F06292", WhiteHexString)); // Pink 300
            DefaultThemes.Add(new ColorTheme("Pink", "#E91E63", WhiteHexString)); // Pink 500
            DefaultThemes.Add(new ColorTheme("Primrose", "#C2185B", WhiteHexString)); // Pink 700
            DefaultThemes.Add(new ColorTheme("Sangria", "#880E4F", WhiteHexString)); // Pink 900

            DefaultThemes.Add(new ColorTheme("Lilac", "#BA68C8", WhiteHexString)); // Purple 300
            DefaultThemes.Add(new ColorTheme("Purple", "#9C27B0", WhiteHexString)); // Purple 500
            DefaultThemes.Add(new ColorTheme("Royal", "#7B1FA2", WhiteHexString)); // Purple 700
            DefaultThemes.Add(new ColorTheme("Plum", "#4A148C", WhiteHexString)); // Purple 900

            DefaultThemes.Add(new ColorTheme("Lavender", "#9575CD", WhiteHexString)); // Deep Purple 300
            DefaultThemes.Add(new ColorTheme("Deep Purple", "#673AB7", WhiteHexString)); // Deep Purple 500
            DefaultThemes.Add(new ColorTheme("Twilight", "#512DA8", WhiteHexString)); // Deep Purple 700

            DefaultThemes.Add(new ColorTheme("Periwinkle", "#5C6BC0", WhiteHexString)); // Indigo 400
            DefaultThemes.Add(new ColorTheme("Indigo", "#3F51B5", WhiteHexString)); // Indigo 500
            DefaultThemes.Add(new ColorTheme("Oceanic", "#303F9F", WhiteHexString)); // Indigo 700
            DefaultThemes.Add(new ColorTheme("Rucesion", "#283593", WhiteHexString)); // Indigo 800
            DefaultThemes.Add(new ColorTheme("Midnight", "#1A237E", WhiteHexString)); // Indigo 900

            DefaultThemes.Add(new ColorTheme("Cerulean", "#1976D2", WhiteHexString)); // Blue 700
            DefaultThemes.Add(new ColorTheme("Classic", "#0D47A1", WhiteHexString, isDefault: true)); // Blue 900

            DefaultThemes.Add(new ColorTheme("Sky", "#039BE5", WhiteHexString)); // Light Blue 600
            DefaultThemes.Add(new ColorTheme("Carolina", "#01579B", WhiteHexString)); // Light Blue 800

            DefaultThemes.Add(new ColorTheme("Cyan", "#0097A7", WhiteHexString)); // Cyan 700
            DefaultThemes.Add(new ColorTheme("Aquamarine", "#00838F", WhiteHexString)); // Cyan 800
            DefaultThemes.Add(new ColorTheme("Spruce", "#006064", WhiteHexString)); // Cyan 900

            DefaultThemes.Add(new ColorTheme("Teal", "#009688", WhiteHexString)); // Teal 500
            DefaultThemes.Add(new ColorTheme("Turquoise", "#00796B", WhiteHexString)); // Teal 700
            DefaultThemes.Add(new ColorTheme("Evergreen", "#004D40", WhiteHexString)); // Teal 900

            DefaultThemes.Add(new ColorTheme("Fresh", "#43A047", WhiteHexString)); // Green 600
            DefaultThemes.Add(new ColorTheme("Mileth", "#2E7D32", WhiteHexString)); // Green 800
            DefaultThemes.Add(new ColorTheme("Forest", "#1B5E20", WhiteHexString)); // Green 900

            DefaultThemes.Add(new ColorTheme("Lime", "#689F38", WhiteHexString)); // Light Green 700
            DefaultThemes.Add(new ColorTheme("Moss", "#33691E", WhiteHexString)); // Light Green 900

            DefaultThemes.Add(new ColorTheme("Lemon", "#9E9D24", WhiteHexString)); // Lime 800
            DefaultThemes.Add(new ColorTheme("Mustard", "#827717", WhiteHexString)); // Lime 900

            DefaultThemes.Add(new ColorTheme("Gold", "#F9A825", WhiteHexString)); // Yellow 800

            DefaultThemes.Add(new ColorTheme("Ranger", "#FF8F00", WhiteHexString)); // Amber 800
            DefaultThemes.Add(new ColorTheme("Tangerine", "#FF6F00", WhiteHexString)); // Amber 900

            DefaultThemes.Add(new ColorTheme("Valve", "#E65100", WhiteHexString)); // Orange 900

            DefaultThemes.Add(new ColorTheme("Coral", "#FF7043", WhiteHexString)); // Deep Orange 400
            DefaultThemes.Add(new ColorTheme("Sanguine", "#BF360C", WhiteHexString)); // Deep Orange 900

            DefaultThemes.Add(new ColorTheme("Cream", "#A1887F", WhiteHexString)); // Brown 300
            DefaultThemes.Add(new ColorTheme("Nude", "#8D6E63", WhiteHexString)); // Brown 400
            DefaultThemes.Add(new ColorTheme("Cappuccino", "#795548", WhiteHexString)); // Brown 500
            DefaultThemes.Add(new ColorTheme("Earth", "#6D4C41", WhiteHexString)); // Brown 600
            DefaultThemes.Add(new ColorTheme("Wood", "#5D4037", WhiteHexString)); // Brown 700
            DefaultThemes.Add(new ColorTheme("Chocolate", "#4E342E", WhiteHexString)); // Brown 800
            DefaultThemes.Add(new ColorTheme("Espresso", "#3E2723", WhiteHexString)); // Brown 900

            DefaultThemes.Add(new ColorTheme("Silver", "#757575", WhiteHexString)); // Grey 600
            DefaultThemes.Add(new ColorTheme("Pewter", "#616161", WhiteHexString)); // Grey 700
            DefaultThemes.Add(new ColorTheme("Gunmetal", "#424242", WhiteHexString)); // Grey 800
            DefaultThemes.Add(new ColorTheme("Charcoal", "#212121", WhiteHexString)); // Grey 900

            DefaultThemes.Add(new ColorTheme("Smoke", "#78909C", WhiteHexString));  // Blue Grey 400
            DefaultThemes.Add(new ColorTheme("Silverstone", "#607D8B", WhiteHexString));  // Blue Grey 500
            DefaultThemes.Add(new ColorTheme("Steel", "#546E7A", WhiteHexString));  // Blue Grey 600
            DefaultThemes.Add(new ColorTheme("Slate", "#455A64", WhiteHexString));  // Blue Grey 700
            DefaultThemes.Add(new ColorTheme("Ebony", "#37474F", WhiteHexString));  // Blue Grey 800
            DefaultThemes.Add(new ColorTheme("Shadow", "#263238", WhiteHexString));  // Blue Grey 900

            DefaultThemes.Add(new ColorTheme("Void", "#000000", WhiteHexString));
        }
        #endregion

        readonly ConcurrentDictionary<String, ColorTheme> colorThemes = new ConcurrentDictionary<string, ColorTheme>(StringComparer.OrdinalIgnoreCase);

        public event ColorThemeEventHandler ThemeAdded;
        public event ColorThemeEventHandler ThemeChanged;
        public event ColorThemeEventHandler ThemeRemoved;

        #region Collection Properties
        public ColorTheme this[string key]
        {
            get { return GetTheme(key); }
            set { AddTheme(value); }
        }
        public int Count { get { return colorThemes.Count; } }

        public IEnumerable<ColorTheme> Themes
        {
            get
            {
                return colorThemes.Values
                  .OrderByDescending(x => x.BackgroundHsv.Hue)
                  .ThenByDescending(x => x.BackgroundHsv.Value);
            }
        }
        #endregion

        #region Collection Methods
        public void AddTheme(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException("theme");

            if (string.IsNullOrWhiteSpace(theme.Name))
                throw new ArgumentException("Key cannot be null or whitespace.", "version");

            var alreadyExists = colorThemes.ContainsKey(theme.Name);
            colorThemes[theme.Name] = theme;

            if (alreadyExists)
                OnThemeChanged(theme);
            else
                OnThemeAdded(theme);
        }

        public ColorTheme GetTheme(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            return colorThemes[key];
        }

        public bool ContainsTheme(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            return colorThemes.ContainsKey(key);
        }

        public bool RemoveTheme(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (!colorThemes.ContainsKey(key))
                return false;

            ColorTheme removedTheme;
            bool wasRemoved = colorThemes.TryRemove(key, out removedTheme);

            if (wasRemoved)
                OnThemeRemoved(removedTheme);

            return wasRemoved;
        }

        public void ClearThemes()
        {
            foreach (var theme in colorThemes.Values)
                OnThemeRemoved(theme);

            colorThemes.Clear();
        }
        #endregion

        #region Load / Save Methods
        public void LoadFromFile(string filename)
        {
            using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                LoadFromStream(inputStream);
            }
        }

        public void LoadFromStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(ColorThemeCollection));
            var collection = serializer.Deserialize(stream) as ColorThemeCollection;

            if (collection != null)
                foreach (var theme in collection.Themes)
                    AddTheme(theme);
        }

        public void SaveToFile(string filename)
        {
            using (var outputStream = File.Create(filename))
            {
                SaveToStream(outputStream);
                outputStream.Flush();
            }
        }

        public void SaveToStream(Stream stream)
        {
            var collection = new ColorThemeCollection(this.Themes);
            var serializer = new XmlSerializer(typeof(ColorThemeCollection));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            serializer.Serialize(stream, collection, namespaces);
        }

        public void LoadDefaultThemes()
        {
            foreach (var theme in DefaultThemes)
                AddTheme(theme);
        }
        #endregion

        public void ApplyTheme(string themeKey)
        {
            if (themeKey == null)
                return;

            if (!colorThemes.ContainsKey(themeKey))
                return;

            var theme = colorThemes[themeKey];

            Application.Current.Resources["ObsidianBackground"] = theme.Background;
            Application.Current.Resources["ObsidianForeground"] = theme.Foreground;
        }

        public void ApplyRainbowMode()
        {
            Application.Current.Resources["ObsidianBackground"] = Application.Current.Resources["RainbowBackground"];
            Application.Current.Resources["ObsidianForeground"] = Application.Current.Resources["RainbowForeground"];
        }

        #region Event Handler Methods
        void OnThemeAdded(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException("theme");

            var handler = this.ThemeAdded;

            if (handler != null)
                handler(this, new ColorThemeEventArgs(theme));
        }

        void OnThemeChanged(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException("theme");

            var handler = this.ThemeChanged;

            if (handler != null)
                handler(this, new ColorThemeEventArgs(theme));
        }

        void OnThemeRemoved(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException("theme");

            var handler = this.ThemeRemoved;

            if (handler != null)
                handler(this, new ColorThemeEventArgs(theme));
        }
        #endregion
    }
}
