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
        private readonly List<ColorTheme> themes = new List<ColorTheme>();

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version { get; set; }

        [XmlIgnore]
        public int Count => themes.Count;

        [XmlArray("Themes")]
        [XmlArrayItem("Theme")]
        public List<ColorTheme> Themes => themes;

        public ColorThemeCollection(IEnumerable<ColorTheme> collection)
        {
            if (collection != null)
                themes.AddRange(collection);
        }

        public override string ToString() => $"Themes = {Count}";
    }
}
