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
    string version;
    List<ColorTheme> themes;

    [XmlAttribute("FileVersion")]
    [DefaultValue(null)]
    public string Version
    {
      get { return version; }
      set { version = value; }
    }

    [XmlIgnore]
    public int Count { get { return themes.Count; } }

    [XmlArray("Themes")]
    [XmlArrayItem("Theme")]
    public List<ColorTheme> Themes
    {
      get { return themes; }
      private set { themes = value; }
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
      themes = new List<ColorTheme>(collection);
    }
  }
}
