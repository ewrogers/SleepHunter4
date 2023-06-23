using System.Xml.Serialization;
using SleepHunter.Common;

namespace SleepHunter.Settings
{
    public sealed class FeatureFlag : ObservableObject
    {
        private string key;
        private bool enabled = true;
        private object state;

        [XmlIgnore]
        public bool HasState => state != null;

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

        public object GetState() => state;

        public T GetState<T>() => (T)state;

        public bool TryGetState<T>(out T state)
        {
            state = default;

            if (this.state is not T typedState)
                return false;

            state = typedState;
            return true;
        }

        public void SetState(object newState)
        {
            state = newState;
            RaisePropertyChanged("State");
        }

        public override string ToString() => $"{Key} = {IsEnabled}";
    }
}
