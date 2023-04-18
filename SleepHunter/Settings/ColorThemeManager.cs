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

        #region Singleton
        static readonly ColorThemeManager instance = new ColorThemeManager();

        public static ColorThemeManager Instance { get { return instance; } }
        #endregion

        private ColorThemeManager() { }

        readonly ConcurrentDictionary<string, ColorTheme> colorThemes = new ConcurrentDictionary<string, ColorTheme>(StringComparer.OrdinalIgnoreCase);

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

        public IEnumerable<ColorTheme> Themes => colorThemes.Values.OrderBy(theme => theme.SortIndex);

        public ColorTheme DefaultTheme => Themes.FirstOrDefault(theme => theme.IsDefault) ?? ColorTheme.DefaultTheme;

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

        public void ApplyDefaultTheme()
        {
            Application.Current.Resources["ObsidianBackground"] = DefaultTheme.Background;
            Application.Current.Resources["ObsidianForeground"] = DefaultTheme.Foreground;
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
