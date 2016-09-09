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
      string name;
      SolidColorBrush background;
      HueSaturationValue backgroundHsv;
      SolidColorBrush foreground;
      HueSaturationValue foregroundHsv;
      bool isDefault;

      [XmlAttribute("Name")]
      public string Name
      {
         get { return name; }
         set { SetProperty(ref name, value, "Name"); }
      }

      [XmlIgnore]
      public SolidColorBrush Background
      {
         get { return background; }
         set
         {
            if (value == null)
               throw new ArgumentNullException("value");

            SetProperty(ref background, value, "Background");
            SetProperty(ref backgroundHsv, new HueSaturationValue(value.Color), "BackgroundHsv");
         }
      }

      [XmlAttribute("Background")]
      public string BackgroundHexColor
      {
         get { return background.Color.ToString(); }
         set
         {
            if (value == null)
               throw new ArgumentNullException("value");

            var color = ColorConverter.ConvertFromString(value);

            if (color is Color)
               this.Background = new SolidColorBrush((Color)color);
            else
               throw new FormatException("Invalid hex color format.");
         }
      }

      [XmlIgnore]
      public byte BackgroundColorRed
      {
         get { return background.Color.R; }
      }

      [XmlIgnore]
      public byte BackgroundColorGreen
      {
         get { return background.Color.G; }
      }

      [XmlIgnore]
      public byte BackgroundColorBlue
      {
         get { return background.Color.B; }
      }

      [XmlIgnore]
      public HueSaturationValue BackgroundHsv
      {
         get { return backgroundHsv; }
      }

      [XmlIgnore]
      public HueSaturationValue ForegroundHsv
      {
         get { return foregroundHsv; }
      }

      [XmlIgnore]
      public double BackgroundValue
      {
         get { return Math.Max(Math.Max(background.Color.R, background.Color.G), background.Color.B); }
      }

      [XmlIgnore]
      public SolidColorBrush Foreground
      {
         get { return foreground; }
         set
         {
            if (value == null)
               throw new ArgumentNullException("value");

            SetProperty(ref foreground, value, "Foreground");
            SetProperty(ref foregroundHsv, new HueSaturationValue(value.Color), "ForegroundHsv");
         }
      }

      [XmlAttribute("Foreground")]
      public string ForegroundHexColor
      {
         get { return foreground.Color.ToString(); }
         set
         {
            if (value == null)
               throw new ArgumentNullException("value");

            var color = ColorConverter.ConvertFromString(value);

            if (color is Color)
               this.Foreground = new SolidColorBrush((Color)color);
            else
               throw new FormatException("Invalid hex color format.");
         }
      }

      [XmlAttribute("IsDefault")]
      [DefaultValue(false)]
      public bool IsDefault
      {
         get { return isDefault; }
         set { SetProperty(ref isDefault, value, "IsDefault"); }
      }

      private ColorTheme()
         : this(string.Empty, Brushes.Black, Brushes.White, false) { }

      public ColorTheme(string key)
         : this(key, Brushes.Black, Brushes.White, false) { }

      public ColorTheme(string name, string backgroundHexColor, string foregroundHexColor, bool isDefault = false)
      {
         this.name = name;
         this.BackgroundHexColor = backgroundHexColor;
         this.ForegroundHexColor = foregroundHexColor;
         this.isDefault = isDefault;
      }

      public ColorTheme(string name, SolidColorBrush background, SolidColorBrush foreground, bool isDefault = false)
      {
         if (name == null)
            throw new ArgumentNullException("name");

         if (background == null)
            throw new ArgumentNullException("background");

         if (foreground == null)
            throw new ArgumentNullException("foreground");

         this.name = name;
         this.background = background;
         this.foreground = foreground;
         this.isDefault = isDefault;
      }

      public override string ToString()
      {
         return this.Name ?? string.Empty;
      }
   }
}
