using System.Xml.Serialization;
using SleepHunter.Common;

namespace SleepHunter.Settings
{
    public sealed class FeatureFlag : ObservableObject
    {
        private string key;
        private bool enabled = true;

        [XmlAttribute("Key")]
        public string Key
        {
            get => key;
            set => SetProperty(ref key, value);
        }

        [XmlAttribute("Enabled")]
        public bool IsEnabled
        {
            get => enabled;
            set => SetProperty(ref enabled, value);
        }

        public FeatureFlag() :
            this("Key")
        { }

        public FeatureFlag(string key, bool enabled = true)
        {
            Key = key;
            IsEnabled = enabled;
        }

        public override string ToString() => $"{Key} = {IsEnabled}";
    }
}
