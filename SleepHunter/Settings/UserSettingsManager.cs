using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace SleepHunter.Settings
{
    internal sealed class UserSettingsManager
    {
        public static readonly string SettingsFile = @"Settings.xml";

        static readonly UserSettingsManager instance = new UserSettingsManager();

        public static UserSettingsManager Instance => instance;

        private UserSettingsManager()
        {
            Settings = new UserSettings();
        }

        private UserSettings settings;

        public event PropertyChangingEventHandler SettingChanging;
        public event PropertyChangedEventHandler SettingChanged;

        public UserSettings Settings
        {
            get { return settings; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

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

            if (!(serializer.Deserialize(stream) is UserSettings settings))
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

            // Update version when saving
            settings.Version = UserSettings.CurrentVersion;
            serializer.Serialize(stream, settings, namespaces);
        }

        private void Settings_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (!(sender is UserSettings settings))
                return;

            OnSettingChanging(settings, e.PropertyName);
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is UserSettings settings))
                return;

            OnSettingChanged(settings, e.PropertyName);
        }

        private void OnSettingChanging(UserSettings settings, string propertyName)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            SettingChanging?.Invoke(settings, new PropertyChangingEventArgs(propertyName));
        }

        private void OnSettingChanged(UserSettings settings, string propertyName)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            SettingChanged?.Invoke(settings, new PropertyChangedEventArgs(propertyName));
        }
    }
}
