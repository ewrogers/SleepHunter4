using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace SleepHunter.Settings
{
    internal sealed class ColorThemeManager
    {
        public static readonly string ThemesFile = @"Themes.xml";

        private static readonly List<ColorTheme> DefaultThemes = new List<ColorTheme>();

        private static readonly ColorThemeManager instance = new ColorThemeManager();

        public static ColorThemeManager Instance => instance;

        private ColorThemeManager()
        {
            var sortIndex = 1;

            // Slate
            DefaultThemes.Add(new ColorTheme("Steel", "#475569", "#f8fafc", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Slate", "#334155", "#f8fafc", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Midnight", "#1e293b", "#f8fafc", sortIndex++));

            // Gray
            DefaultThemes.Add(new ColorTheme("Battleship", "#4b5563", "#f9fafb", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Stealth", "#374151", "#f9fafb", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Lead", "#1f2937", "#f9fafb", sortIndex++));

            // Zinc
            DefaultThemes.Add(new ColorTheme("Silver", "#52525b", "#fafafa", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Zinc", "#3f3f46", "#fafafa", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Gunmetal", "#27272a", "#fafafa", sortIndex++));

            // Neutral
            DefaultThemes.Add(new ColorTheme("Tin", "#525252", "#fafafa", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Pewter", "#404040", "#fafafa", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Carbon", "#262626", "#fafafa", sortIndex++));

            // Stone
            DefaultThemes.Add(new ColorTheme("Stone", "#57534e", "#fafaf9", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Taupe", "#44403c", "#fafaf9", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Coffee", "#292524", "#fafaf9", sortIndex++));

            // Red
            DefaultThemes.Add(new ColorTheme("Cherry", "#dc2626", "#fef2f2", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Crimson", "#b91c1c", "#fef2f2", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Ruby", "#991b1b", "#fef2f2", sortIndex++));

            // Orange
            DefaultThemes.Add(new ColorTheme("Tangerine", "#ea580c", "#fff7ed", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Orange", "#c2410c", "#fff7ed", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Rust", "#9a3412", "#fff7ed", sortIndex++));

            // Amber
            DefaultThemes.Add(new ColorTheme("Citrine", "#d97706", "#fffbeb", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Amber", "#b45309", "#fffbeb", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Harvest", "#92400e", "#fffbeb", sortIndex++));

            // Yellow
            DefaultThemes.Add(new ColorTheme("Topaz", "#ca8a04", "#fefce8", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Wheat", "#a16207", "#fefce8", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Leather", "#854d0e", "#fefce8", sortIndex++));

            // Lime
            DefaultThemes.Add(new ColorTheme("Peridot", "#65a30d", "#f7fee7", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Lime", "#4d7c0f", "#f7fee7", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Moss", "#3f6212", "#f7fee7", sortIndex++));

            // Green
            DefaultThemes.Add(new ColorTheme("Jade", "#16a34a", "#f0fdf4", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Grass", "#15803d", "#f0fdf4", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Forest", "#166534", "#f0fdf4", sortIndex++));

            // Emerald
            DefaultThemes.Add(new ColorTheme("Seafoam", "#059669", "#ecfdf5", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Emerald", "#047857", "#ecfdf5", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Turquoise", "#065f46", "#ecfdf5", sortIndex++));

            // Cyan
            DefaultThemes.Add(new ColorTheme("Air", "#0891b2", "#ecfeff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Cerulean", "#0e7490", "#ecfeff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Teal", "#155e75", "#ecfeff", sortIndex++));

            // Sky
            DefaultThemes.Add(new ColorTheme("Sky", "#0284c7", "#f0f9ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Azure", "#0369a1", "#f0f9ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Stormy", "#075985", "#f0f9ff", sortIndex++));

            // Blue
            DefaultThemes.Add(new ColorTheme("Cornflower", "#2563eb", "#eff6ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Lapis", "#1d4ed8", "#eff6ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Sapphire", "#1e40af", "#eff6ff", sortIndex++, true));

            // Indigo
            DefaultThemes.Add(new ColorTheme("Cornflower", "#4f46e5", "#eef2ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Indigo", "#4338ca", "#eef2ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Twilight", "#3730a3", "#eef2ff", sortIndex++));

            // Violet
            DefaultThemes.Add(new ColorTheme("Lavender", "#7c3aed", "#f5f3ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Nightshade", "#6d28d9", "#f5f3ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Amethyst", "#5b21b6", "#f5f3ff", sortIndex++));

            // Purple
            DefaultThemes.Add(new ColorTheme("Lilac", "#9333ea", "#faf5ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Grape", "#7e22ce", "#faf5ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Royal", "#6b21a8", "#faf5ff", sortIndex++));

            // Fuchsia
            DefaultThemes.Add(new ColorTheme("Bubblegum", "#c026d3", "#fdf4ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Orchid", "#a21caf", "#fdf4ff", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Wine", "#86198f", "#fdf4ff", sortIndex++));

            // Pink
            DefaultThemes.Add(new ColorTheme("Peony", "#db2777", "#fdf2f8", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Strawberry", "#be185d", "#fdf2f8", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Raspberry", "#9d174d", "#fdf2f8", sortIndex++));

            // Rose
            DefaultThemes.Add(new ColorTheme("Vermillion", "#e11d48", "#fdf2f8", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Rose", "#be123c", "#fdf2f8", sortIndex++));
            DefaultThemes.Add(new ColorTheme("Garnet", "#9f1239", "#fdf2f8", sortIndex++));
        }

        private readonly ConcurrentDictionary<string, ColorTheme> colorThemes = new ConcurrentDictionary<string, ColorTheme>(StringComparer.OrdinalIgnoreCase);

        public event ColorThemeEventHandler ThemeAdded;
        public event ColorThemeEventHandler ThemeChanged;
        public event ColorThemeEventHandler ThemeRemoved;

        public ColorTheme this[string key]
        {
            get { return GetTheme(key); }
            set { AddTheme(value); }
        }
        public int Count { get { return colorThemes.Count; } }

        public IEnumerable<ColorTheme> Themes => colorThemes.Values.OrderBy(theme => theme.SortIndex);

        public ColorTheme DefaultTheme => Themes.FirstOrDefault(theme => theme.IsDefault);

        public void AddTheme(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            if (string.IsNullOrWhiteSpace(theme.Name))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(theme.Name));

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
                throw new ArgumentNullException(nameof(key));

            return colorThemes[key];
        }

        public bool ContainsTheme(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return colorThemes.ContainsKey(key);
        }

        public bool RemoveTheme(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!colorThemes.ContainsKey(key))
                return false;

            bool wasRemoved = colorThemes.TryRemove(key, out var removedTheme);

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

            if (serializer.Deserialize(stream) is ColorThemeCollection collection)
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

        private void OnThemeAdded(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            ThemeAdded?.Invoke(this, new ColorThemeEventArgs(theme));
        }

        private void OnThemeChanged(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

           ThemeChanged?.Invoke(this, new ColorThemeEventArgs(theme));
        }

        private void OnThemeRemoved(ColorTheme theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            ThemeRemoved?.Invoke(this, new ColorThemeEventArgs(theme));
        }
    }
}
