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

      #region Default Themes
      static readonly List<ColorTheme> DefaultThemes = new List<ColorTheme>();
      #endregion

      #region Singleton
      static readonly ColorThemeManager instance = new ColorThemeManager();

      public static ColorThemeManager Instance { get { return instance; } }

      private ColorThemeManager()
      {
         DefaultThemes.Add(new ColorTheme("Sleepy Blue", "#FF104898", "#FFF8F8F8", isDefault:true));
         DefaultThemes.Add(new ColorTheme("Firestorm", "#FFA81018", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Maroon", "#FF78081F", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Valve", "#FFD84010", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Ranger Danger", "#FFC86810", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Rust", "#FF682808", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Lemon", "#FFA4A408", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Gold", "#FFA87F1F", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Spring", "#FF4F9818", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Lime", "#FF20A810", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Moist Moss", "#FF106808", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Emerald", "#FF083818", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Sea Foam", "#FF1F8868", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Teal", "#FF105848", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Ice", "#FF60B0D0", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Sky", "#FF1094B8", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Lapis Lazuli", "#FF1858C8", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Oceanic", "#FF083078", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Lavender", "#FF404090", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Indigo", "#FF6824C8", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Arcane", "#FF681888", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Royal Purple", "#FF401070", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Cotton Candy", "#FFC87898", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Magenta", "#FFC83098", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Pretty in Pink", "#FFDF2878", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Wood", "#FF4F1F08", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Sand", "#FF8F6F48", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Steel", "#FF808080", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Smoke", "#FF606060", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Shadow", "#FF404040", "#FFF8F8F8"));
         DefaultThemes.Add(new ColorTheme("Void", "#FF101010", "#FFF8F8F8"));
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
            return from t in colorThemes.Values
                   orderby t.BackgroundHsv.Hue descending, t.BackgroundHsv.Value descending
                   select t;
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
            inputStream.Close();
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
            outputStream.Close();
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
         if (themeKey == null) return;

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
