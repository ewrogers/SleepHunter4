using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace SleepHunter.Settings
{
    public sealed class UserSettingsManager
   {
      public static readonly string SettingsFile = @"Settings.xml";      

      #region Singleton
      static readonly UserSettingsManager instance = new UserSettingsManager();

      public static UserSettingsManager Instance { get { return instance; } }

      private UserSettingsManager()
      {
         this.Settings = new UserSettings();
      }
      #endregion

      UserSettings settings;

      public event PropertyChangingEventHandler SettingChanging;
      public event PropertyChangedEventHandler SettingChanged;

      public UserSettings Settings
      {
         get { return settings; }
         private set
         {
            if (value == null)
               throw new ArgumentNullException("value");

            if (settings == value)
               return;

            if (settings != null)
            {
               settings.PropertyChanging -= Settings_PropertyChanging;
               settings.PropertyChanged -= Settings_PropertyChanged;
            }

            settings = value;

            if (settings != null)
            {
               settings.PropertyChanging += Settings_PropertyChanging;
               settings.PropertyChanged += Settings_PropertyChanged;
            }
         }
      }

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
         var serializer = new XmlSerializer(typeof(UserSettings));
         var settings = serializer.Deserialize(stream) as UserSettings;

         if (settings == null)
            settings = new UserSettings();

         Settings = settings;
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
         var serializer = new XmlSerializer(typeof(UserSettings));
         var namespaces = new XmlSerializerNamespaces();
         namespaces.Add("", "");

         serializer.Serialize(stream, settings, namespaces);
      }
      #endregion

      #region Event Handler Methods
      void Settings_PropertyChanging(object sender, PropertyChangingEventArgs e)
      {
         var settings = sender as UserSettings;
         if (settings == null) return;

         OnSettingChanging(settings, e.PropertyName);
      }

      void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         var settings = sender as UserSettings;
         if (settings == null) return;

         OnSettingChanged(settings, e.PropertyName);
      }      

      void OnSettingChanging(UserSettings settings, string propertyName)
      {
         if(settings==null)
            throw new ArgumentNullException("settings");

         if(propertyName==null)
            throw new ArgumentNullException("propertyName");

         var handler = this.SettingChanging;

         if (handler != null)
            handler(settings, new PropertyChangingEventArgs(propertyName));
      }

      void OnSettingChanged(UserSettings settings, string propertyName)
      {
         if (settings == null)
            throw new ArgumentNullException("settings");

         if (propertyName == null)
            throw new ArgumentNullException("propertyName");

         var handler = this.SettingChanged;

         if (handler != null)
            handler(settings, new PropertyChangedEventArgs(propertyName));
      }
      #endregion
   }
}
