using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SleepHunter.Settings
{
    [Serializable]
    [XmlRoot("ClientLayout")]
    public sealed class ClientLayout : ObservableObject
    {
        private string pointerWidth = "Bit32";
        private string executableName;
        private string windowClassName;
        private string windowTitle;
        private bool supportsFlowering;
        private bool supportsLoginNotificationSuppression;
        private bool supportsModifiersKeyFix;
        private bool supportsAltToShowGroundItems;
        private bool supportsImprovedAutoFollow;
        private bool supportsItemQuantitiesInDialogs;
        private bool supportsDraggableExchangeDialog;
        private bool supportsExchangeResultsInMessageBar;
        private long multipleInstanceAddress;
        private long introVideoAddress;
        private long noWallAddress;

        [XmlAttribute]
        public string PointerWidth
        {
            get => pointerWidth;
            set => SetProperty(ref pointerWidth, value);
        }

        [XmlElement]
        public string ExecutableName
        {
            get => executableName;
            set => SetProperty(ref executableName, value);
        }

        [XmlElement]
        public string WindowClassName
        {
            get => windowClassName;
            set => SetProperty(ref windowClassName, value);
        }

        [XmlElement]
        public string WindowTitle
        {
            get => windowTitle;
            set => SetProperty(ref windowTitle, value);
        }

        [XmlElement]
        public bool SupportsFlowering
        {
            get => supportsFlowering;
            set => SetProperty(ref supportsFlowering, value);
        }

        [XmlElement]
        public bool SupportsLoginNotificationSuppression
        {
            get => supportsLoginNotificationSuppression;
            set => SetProperty(ref supportsLoginNotificationSuppression, value);
        }

        [XmlElement]
        public bool SupportsModifiersKeyFix
        {
            get => supportsModifiersKeyFix;
            set => SetProperty(ref supportsModifiersKeyFix, value);
        }

        [XmlElement]
        public bool SupportsAltToShowGroundItems
        {
            get => supportsAltToShowGroundItems;
            set => SetProperty(ref supportsAltToShowGroundItems, value);
        }

        [XmlElement]
        public bool SupportsImprovedAutoFollow
        {
            get => supportsImprovedAutoFollow;
            set => SetProperty(ref supportsImprovedAutoFollow, value);
        }

        [XmlElement]
        public bool SupportsItemQuantitiesInDialogs
        {
            get => supportsItemQuantitiesInDialogs;
            set => SetProperty(ref supportsItemQuantitiesInDialogs, value);
        }

        [XmlElement]
        public bool SupportsDraggableExchangeDialog
        {
            get => supportsDraggableExchangeDialog;
            set => SetProperty(ref supportsDraggableExchangeDialog, value);
        }

        [XmlElement]
        public bool SupportsExchangeResultsInMessageBar
        {
            get => supportsExchangeResultsInMessageBar;
            set => SetProperty(ref supportsExchangeResultsInMessageBar, value);
        }

        [XmlIgnore]
        public long MultipleInstanceAddress
        {
            get => multipleInstanceAddress;
            set
            {
                if (SetProperty(ref multipleInstanceAddress, value))
                    OnPropertyChanged(nameof(MultipleInstanceAddressHex));
            }
        }

        [XmlElement("MultipleInstanceAddress")]
        [DefaultValue("0")]
        public string MultipleInstanceAddressHex
        {
            get => multipleInstanceAddress.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    MultipleInstanceAddress = parsedLong;
            }
        }

        [XmlIgnore]
        public long IntroVideoAddress
        {
            get => introVideoAddress;
            set
            {
                if (SetProperty(ref introVideoAddress, value))
                    OnPropertyChanged(nameof(IntroVideoAddressHex));
            }
        }

        [XmlElement("IntroVideoAddress")]
        [DefaultValue("0")]
        public string IntroVideoAddressHex
        {
            get => introVideoAddress.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    IntroVideoAddress = parsedLong;
            }
        }

        [XmlIgnore]
        public long NoWallAddress
        {
            get => noWallAddress;
            set
            {
                if (SetProperty(ref noWallAddress, value))
                    OnPropertyChanged(nameof(NoWallAddressHex));
            }
        }

        [XmlElement("NoWallAddress")]
        [DefaultValue("0")]
        public string NoWallAddressHex
        {
            get => noWallAddress.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    NoWallAddress = parsedLong;
            }
        }

        public ClientLayout()
        {
        }
    }
}
