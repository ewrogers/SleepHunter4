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
        public const string ThemesFile = @"Themes.xml";

        private static readonly ColorThemeManager instance = new();
        public static ColorThemeManager Instance => instance;

        private ColorThemeManager() { }

        private readonly ConcurrentDictionary<string, ColorTheme> colorThemes = new(StringComparer.OrdinalIgnoreCase);

        public event ColorThemeEventHandler ThemeAdded;
        public event ColorThemeEventHandler ThemeChanged;
        public event ColorThemeEventHandler ThemeRemoved;

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

            var theme = colorThemes[themeKey];

            Application.Current.Resources["ObsidianBackground"] = theme.Background;
            Application.Current.Resources["ObsidianForeground"] = theme.Foreground;
        }

        public void ApplyDefaultTheme()
        {
            Application.Current.Resources["ObsidianBackground"] = DefaultTheme.Background;
            Application.Current.Resources["ObsidianForeground"] = DefaultTheme.Foreground;
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
