using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SleepHunter.Settings
{
    [Serializable]
    [XmlRoot("ColorThemes")]
    public sealed class ColorThemeCollection
    {
        private string version;
        private List<ColorTheme> themes;

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version
        {
            get => version;
            set => version = value;
        }

        [XmlIgnore]
        public int Count => themes.Count;

        [XmlArray("Themes")]
        [XmlArrayItem("Theme")]
        public List<ColorTheme> Themes
        {
            get => themes;
            private set => themes = value;
        }

        public ColorThemeCollection()
        {
            themes = new List<ColorTheme>();
        }

        public ColorThemeCollection(int capacity)
        {
            themes = new List<ColorTheme>(capacity);
        }

        public ColorThemeCollection(IEnumerable<ColorTheme> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            themes = new List<ColorTheme>(collection);
        }

        public override string ToString() => $"Count = {Count}";
    }
}
