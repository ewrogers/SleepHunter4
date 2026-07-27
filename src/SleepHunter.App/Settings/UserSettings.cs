using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation;
using SleepHunter.ViewModels.Editing;

namespace SleepHunter.Settings
{
    [Serializable]
    [XmlRoot("UserSettings")]
    public class UserSettings : ObservableObject
    {
        public static readonly string CurrentVersion = "1.7";

        private bool isDebugMode;
        private string version;

        // Regular Settings
        private TimeSpan processUpdateInterval;
        private TimeSpan clientUpdateInterval;
        private bool saveMacroStates;
        private string selectedTheme;

        private ClientSortOrder clientSortOrder =
            ClientSortOrder.LaunchOrder;
        private bool showActiveEffects = true;
        private bool suppressLoginNotification = true;
        private bool applyModifiersKeyFix = true;
        private bool allowAltToShowGroundItems = true;
        private bool improvedAutoFollow = true;
        private int improvedAutoFollowMinimumDistance = 3;
        private bool showItemQuantitiesInDialogs = true;
        private bool makeExchangeDialogDraggable = true;
        private bool showExchangeResultsInMessageBar;
        private double inventoryIconSize = 46;
        private double skillIconSize = 46;
        private int inventoryGridWidth = 12;
        private int skillGridWidth = 12;
        private int worldSkillGridWidth = 6;
        private int spellGridWidth = 12;
        private int worldSpellGridWidth = 6;
        private bool showSkillNames;
        private bool showSkillLevels;
        private bool showSpellNames;
        private bool showSpellLevels;

        private string clientPath;
        private string iconDataFile;
        private string paletteDataFile;
        private string skillIconFile;
        private string skillPaletteFile;
        private string spellIconFile;
        private string spellPaletteFile;

        private bool allowMultipleInstances = true;
        private bool skipintroVideo = true;
        private bool noWalls = false;

        private ObservationChangeAction mapChangeAction;
        private ObservationChangeAction coordsChangeAction;

        private bool useShiftForMedeniaPane;
        private bool preserveUserPanel;
        private bool useSpaceForAssail;
        private bool disarmForAssails;

        private SpellRotationMode spellRotationMode;
        private TimeSpan zeroLineDelay;
        private TimeSpan singleLineDelay;
        private TimeSpan multipleLineDelay;
        private bool useFasSpiorad;
        private bool useFasSpioradOnDemand;
        private double fasSpioradThreshold;
        private bool requireManaForSpells;
        private bool allowStaffSwitching;
        private bool warnOnDuplicateSpells;
        private bool skipSpellsOnCooldown = true;

        private bool flowerAltsFirst;
        private bool flowerBeforeSpellMacros;
        private bool flowerHasMinimum;
        private int flowerMinimumMana;
        private bool autoUpdateEnabled;

        // debug settings
        private bool loggingEnabled;
        private bool showAllProcesses;

        [XmlIgnore]
        public bool IsDebugMode
        {
            get => isDebugMode;
            set => SetProperty(ref isDebugMode, value);
        }

        [XmlAttribute("Version")]
        public string Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        [XmlIgnore]
        public TimeSpan ProcessUpdateInterval
        {
            get => processUpdateInterval;
            set
            {
                if (SetProperty(ref processUpdateInterval, value))
                    OnPropertyChanged(
                        nameof(ProcessUpdateIntervalSeconds));
            }
        }

        [XmlElement("ProcessUpdateInterval")]
        public double ProcessUpdateIntervalSeconds
        {
            get => processUpdateInterval.TotalSeconds;
            set => ProcessUpdateInterval = TimeSpan.FromSeconds(value);
        }

        [XmlIgnore]
        public TimeSpan ClientUpdateInterval
        {
            get => clientUpdateInterval;
            set
            {
                if (SetProperty(ref clientUpdateInterval, value))
                    OnPropertyChanged(
                        nameof(ClientUpdateIntervalSeconds));
            }
        }

        [XmlElement("ClientUpdateInterval")]
        public double ClientUpdateIntervalSeconds
        {
            get => clientUpdateInterval.TotalSeconds;
            set => ClientUpdateInterval = TimeSpan.FromSeconds(value);
        }

        [XmlElement("SaveMacroStates")]
        public bool SaveMacroStates
        {
            get => saveMacroStates;
            set => SetProperty(ref saveMacroStates, value);
        }

        [XmlElement("SelectedTheme")]
        public string SelectedTheme
        {
            get => selectedTheme;
            set => SetProperty(ref selectedTheme, value);
        }

        [XmlElement("ClientSortOrder")]
        public ClientSortOrder ClientSortOrder
        {
            get => clientSortOrder;
            set => SetProperty(ref clientSortOrder, value);
        }

        [XmlElement("ShowActiveEffects")]
        public bool ShowActiveEffects
        {
            get => showActiveEffects;
            set => SetProperty(ref showActiveEffects, value);
        }

        [XmlElement("InventoryIconSize")]
        public double InventoryIconSize
        {
            get => inventoryIconSize;
            set => SetProperty(ref inventoryIconSize, value);
        }

        [XmlElement("SkillIconSize")]
        public double SkillIconSize
        {
            get => skillIconSize;
            set => SetProperty(ref skillIconSize, value);
        }

        [XmlElement("InventoryGridWidth")]
        public int InventoryGridWidth
        {
            get => inventoryGridWidth;
            set => SetProperty(ref inventoryGridWidth, value);
        }

        [XmlElement("SkillGridWidth")]
        public int SkillGridWidth
        {
            get => skillGridWidth;
            set => SetProperty(ref skillGridWidth, value);
        }

        [XmlElement("WorldSkillGridWidth")]
        public int WorldSkillGridWidth
        {
            get => worldSkillGridWidth;
            set => SetProperty(ref worldSkillGridWidth, value);
        }

        [XmlElement("SpellGridWidth")]
        public int SpellGridWidth
        {
            get => spellGridWidth;
            set => SetProperty(ref spellGridWidth, value);
        }

        [XmlElement("WorldSpellGridWidth")]
        public int WorldSpellGridWidth
        {
            get => worldSpellGridWidth;
            set => SetProperty(ref worldSpellGridWidth, value);
        }

        [XmlElement("ShowSkillNames")]
        public bool ShowSkillNames
        {
            get => showSkillNames;
            set => SetProperty(ref showSkillNames, value);
        }

        [XmlElement("ShowSkillLevels")]
        public bool ShowSkillLevels
        {
            get => showSkillLevels;
            set => SetProperty(ref showSkillLevels, value);
        }

        [XmlElement("ShowSpellNames")]
        public bool ShowSpellNames
        {
            get => showSpellNames;
            set => SetProperty(ref showSpellNames, value);
        }

        [XmlElement("ShowSpellLevels")]
        public bool ShowSpellLevels
        {
            get => showSpellLevels;
            set => SetProperty(ref showSpellLevels, value);
        }

        [XmlElement("ClientPath")]
        public string ClientPath
        {
            get => clientPath;
            set => SetProperty(ref clientPath, value);
        }

        [XmlIgnore]
        public string IconDataFilePath
        {
            get
            {
                var clientFolder = Path.GetDirectoryName(clientPath);
                var iconDataFilePath = iconDataFile;

                if (!Path.IsPathRooted(iconDataFilePath))
                    iconDataFilePath = Path.Combine(clientFolder, iconDataFilePath);

                return iconDataFilePath;
            }
        }

        [XmlElement("IconDataFile")]
        public string IconDataFile
        {
            get => iconDataFile;
            set
            {
                if (SetProperty(ref iconDataFile, value))
                    OnPropertyChanged(nameof(IconDataFilePath));
            }
        }

        [XmlIgnore]
        public string PaletteDataFilePath
        {
            get
            {
                var clientFolder = Path.GetDirectoryName(clientPath);
                var paletteDataFilePath = paletteDataFile;

                if (!Path.IsPathRooted(paletteDataFilePath))
                    paletteDataFilePath = Path.Combine(clientFolder, paletteDataFilePath);

                return paletteDataFilePath;
            }
        }

        [XmlElement("PaletteDataFile")]
        public string PaletteDataFile
        {
            get => paletteDataFile;
            set
            {
                if (SetProperty(ref paletteDataFile, value))
                    OnPropertyChanged(nameof(PaletteDataFilePath));
            }
        }

        [XmlElement("SkillIconFile")]
        public string SkillIconFile
        {
            get => skillIconFile;
            set => SetProperty(ref skillIconFile, value);
        }

        [XmlElement("SkillPaletteFile")]
        public string SkillPaletteFile
        {
            get => skillPaletteFile;
            set => SetProperty(ref skillPaletteFile, value);
        }

        [XmlElement("SpellIconFile")]
        public string SpellIconFile
        {
            get => spellIconFile;
            set => SetProperty(ref spellIconFile, value);
        }

        [XmlElement("SpellPaletteFile")]
        public string SpellPaletteFile
        {
            get => spellPaletteFile;
            set => SetProperty(ref spellPaletteFile, value);
        }

        [XmlElement("AllowMultipleInstances")]
        public bool AllowMultipleInstances
        {
            get => allowMultipleInstances;
            set => SetProperty(ref allowMultipleInstances, value);
        }

        [XmlElement("SkipIntroVideo")]
        public bool SkipIntroVideo
        {
            get => skipintroVideo;
            set => SetProperty(ref skipintroVideo, value);
        }

        [XmlElement("SuppressLoginNotification")]
        public bool SuppressLoginNotification
        {
            get => suppressLoginNotification;
            set => SetProperty(ref suppressLoginNotification, value);
        }

        [XmlElement("ApplyModifiersKeyFix")]
        public bool ApplyModifiersKeyFix
        {
            get => applyModifiersKeyFix;
            set => SetProperty(ref applyModifiersKeyFix, value);
        }

        [XmlElement("AllowAltToShowGroundItems")]
        public bool AllowAltToShowGroundItems
        {
            get => allowAltToShowGroundItems;
            set => SetProperty(ref allowAltToShowGroundItems, value);
        }

        [XmlElement("ImprovedAutoFollow")]
        public bool ImprovedAutoFollow
        {
            get => improvedAutoFollow;
            set => SetProperty(ref improvedAutoFollow, value);
        }

        [XmlElement("ImprovedAutoFollowMinimumDistance")]
        public int ImprovedAutoFollowMinimumDistance
        {
            get => improvedAutoFollowMinimumDistance;
            set => SetProperty(ref improvedAutoFollowMinimumDistance, Math.Clamp(value, 1, 10));
        }

        [XmlElement("ShowItemQuantitiesInDialogs")]
        public bool ShowItemQuantitiesInDialogs
        {
            get => showItemQuantitiesInDialogs;
            set => SetProperty(ref showItemQuantitiesInDialogs, value);
        }

        [XmlElement("MakeExchangeDialogDraggable")]
        public bool MakeExchangeDialogDraggable
        {
            get => makeExchangeDialogDraggable;
            set => SetProperty(ref makeExchangeDialogDraggable, value);
        }

        [XmlElement("ShowExchangeResultsInMessageBar")]
        public bool ShowExchangeResultsInMessageBar
        {
            get => showExchangeResultsInMessageBar;
            set => SetProperty(ref showExchangeResultsInMessageBar, value);
        }

        [XmlElement("NoWalls")]
        public bool NoWalls
        {
            get => noWalls;
            set => SetProperty(ref noWalls, value);
        }

        [XmlIgnore]
        public ObservationChangeAction MapChangeAction
        {
            get => mapChangeAction;
            set => SetProperty(ref mapChangeAction, value);
        }

        [XmlIgnore]
        public ObservationChangeAction CoordsChangeAction
        {
            get => coordsChangeAction;
            set => SetProperty(ref coordsChangeAction, value);
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("MapChangeAction")]
        public string SerializedMapChangeAction
        {
            get => MapChangeAction.ToString();
            set => MapChangeAction = ParseObservationChangeAction(
                value,
                mapChangeAction);
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("CoordsChangeAction")]
        public string SerializedCoordsChangeAction
        {
            get => CoordsChangeAction.ToString();
            set => CoordsChangeAction = ParseObservationChangeAction(
                value,
                coordsChangeAction);
        }

        [XmlElement("UseShiftForMedeniaPane")]
        public bool UseShiftForMedeniaPane
        {
            get => useShiftForMedeniaPane;
            set => SetProperty(ref useShiftForMedeniaPane, value);
        }

        [XmlElement("PreserveUserPanel")]
        public bool PreserveUserPanel
        {
            get => preserveUserPanel;
            set => SetProperty(ref preserveUserPanel, value);
        }

        [XmlElement("UseSpaceForAssail")]
        public bool UseSpaceForAssail
        {
            get => useSpaceForAssail;
            set => SetProperty(ref useSpaceForAssail, value);
        }

        [XmlElement("DisarmForAssails")]
        public bool DisarmForAssails
        {
            get => disarmForAssails;
            set => SetProperty(ref disarmForAssails, value);
        }

        [XmlElement("SpellRotationMode")]
        public SpellRotationMode SpellRotationMode
        {
            get => spellRotationMode;
            set => SetProperty(ref spellRotationMode, value);
        }

        [XmlIgnore]
        public TimeSpan ZeroLineDelay
        {
            get => zeroLineDelay;
            set
            {
                if (SetProperty(ref zeroLineDelay, value))
                    OnPropertyChanged(nameof(ZeroLineDelaySeconds));
            }
        }

        [XmlElement("ZeroLineDelay")]
        public double ZeroLineDelaySeconds
        {
            get => zeroLineDelay.TotalSeconds;
            set => ZeroLineDelay = TimeSpan.FromSeconds(value);
        }

        [XmlIgnore]
        public TimeSpan SingleLineDelay
        {
            get => singleLineDelay;
            set
            {
                if (SetProperty(ref singleLineDelay, value))
                    OnPropertyChanged(nameof(SingleLineDelaySeconds));
            }
        }

        [XmlElement("SingleLineDelay")]
        public double SingleLineDelaySeconds
        {
            get => singleLineDelay.TotalSeconds;
            set => SingleLineDelay = TimeSpan.FromSeconds(value);
        }

        [XmlIgnore]
        public TimeSpan MultipleLineDelay
        {
            get => multipleLineDelay;
            set
            {
                if (SetProperty(ref multipleLineDelay, value))
                    OnPropertyChanged(
                        nameof(MultipleLineDelaySeconds));
            }
        }

        [XmlElement("MultipleLineDelay")]
        public double MultipleLineDelaySeconds
        {
            get => multipleLineDelay.TotalSeconds;
            set => MultipleLineDelay = TimeSpan.FromSeconds(value);
        }

        [XmlElement("UseFasSpiorad")]
        public bool UseFasSpiorad
        {
            get => useFasSpiorad;
            set => SetProperty(ref useFasSpiorad, value);
        }

        [XmlElement("UseFasSpioradOnDemand")]
        public bool UseFasSpioradOnDemand
        {
            get => useFasSpioradOnDemand;
            set => SetProperty(ref useFasSpioradOnDemand, value);
        }

        [XmlElement("FasSpioradThreshold")]
        public double FasSpioradThreshold
        {
            get => fasSpioradThreshold;
            set => SetProperty(ref fasSpioradThreshold, value);
        }

        [XmlElement("RequireManaForSpells")]
        public bool RequireManaForSpells
        {
            get => requireManaForSpells;
            set => SetProperty(ref requireManaForSpells, value);
        }

        [XmlElement("AllowStaffSwitching")]
        public bool AllowStaffSwitching
        {
            get => allowStaffSwitching;
            set => SetProperty(ref allowStaffSwitching, value);
        }

        [XmlElement("WarnOnDuplicateSpells")]
        public bool WarnOnDuplicateSpells
        {
            get => warnOnDuplicateSpells;
            set => SetProperty(ref warnOnDuplicateSpells, value);
        }

        [XmlElement("SkipSpellsOnCooldown")]
        public bool SkipSpellsOnCooldown
        {
            get => skipSpellsOnCooldown;
            set => SetProperty(ref skipSpellsOnCooldown, value);
        }

        [XmlElement("FlowerAltsFirst")]
        public bool FlowerAltsFirst
        {
            get => flowerAltsFirst;
            set => SetProperty(ref flowerAltsFirst, value);
        }

        [XmlElement("FlowerBeforeSpellMacros")]
        public bool FlowerBeforeSpellMacros
        {
            get => flowerBeforeSpellMacros;
            set => SetProperty(ref flowerBeforeSpellMacros, value);
        }

        [XmlElement("FlowerHasMinimum")]
        public bool FlowerHasMinimum
        {
            get => flowerHasMinimum;
            set => SetProperty(ref flowerHasMinimum, value);
        }

        [XmlElement("FlowerMinimumMana")]
        public int FlowerMinimumMana
        {
            get => flowerMinimumMana;
            set => SetProperty(ref flowerMinimumMana, value);
        }

        [XmlElement("CheckForUpdates")]
        public bool AutoUpdateEnabled
        {
            get => autoUpdateEnabled;
            set => SetProperty(ref autoUpdateEnabled, value);
        }

        [XmlElement("LoggingEnabled")]
        public bool LoggingEnabled
        {
            get => loggingEnabled;
            set => SetProperty(ref loggingEnabled, value);
        }

        [XmlElement("ShowAllProcesses")]
        public bool ShowAllProcesses
        {
            get => showAllProcesses;
            set => SetProperty(ref showAllProcesses, value);
        }

        public UserSettings() { }

        public void ResetDefaults()
        {
            Version = CurrentVersion;

            ProcessUpdateInterval = TimeSpan.FromSeconds(1);
            ClientUpdateInterval = TimeSpan.FromSeconds(0.2);
            SaveMacroStates = true;

            SelectedTheme =
                ColorTheme.DefaultTheme?.Name ?? "Default";
            clientSortOrder = ClientSortOrder.LaunchOrder;
            ShowActiveEffects = true;
            InventoryIconSize = 46;
            SkillIconSize = 46;
            InventoryGridWidth = 12;
            SkillGridWidth = 12;
            WorldSkillGridWidth = 6;
            SpellGridWidth = 12;
            WorldSpellGridWidth = 6;
            ShowSkillNames = false;
            ShowSkillLevels = true;
            ShowSpellNames = false;
            ShowSpellLevels = true;

            ClientPath = FindExecutablePath();
            IconDataFile = "setoa.dat";
            PaletteDataFile = "setoa.dat";
            SkillIconFile = "skill001.epf";
            SkillPaletteFile = "gui06.pal";
            SpellIconFile = "spell001.epf";
            SpellPaletteFile = "gui06.pal";

            AllowMultipleInstances = true;
            SkipIntroVideo = true;
            SuppressLoginNotification = true;
            ApplyModifiersKeyFix = true;
            AllowAltToShowGroundItems = true;
            ImprovedAutoFollow = true;
            ImprovedAutoFollowMinimumDistance = 3;
            ShowItemQuantitiesInDialogs = true;
            MakeExchangeDialogDraggable = true;
            ShowExchangeResultsInMessageBar = false;
            NoWalls = false;

            MapChangeAction = ObservationChangeAction.Stop;
            CoordsChangeAction = ObservationChangeAction.Continue;
            UseShiftForMedeniaPane = true;
            PreserveUserPanel = true;

            UseSpaceForAssail = true;
            DisarmForAssails = true;

            SpellRotationMode = SpellRotationMode.Singular;
            ZeroLineDelay = TimeSpan.FromSeconds(0.2);
            SingleLineDelay = TimeSpan.FromSeconds(1);
            MultipleLineDelay = TimeSpan.FromSeconds(1);
            UseFasSpiorad = false;
            FasSpioradThreshold = 1000;
            UseFasSpioradOnDemand = true;
            RequireManaForSpells = true;
            AllowStaffSwitching = true;
            WarnOnDuplicateSpells = true;
            SkipSpellsOnCooldown = true;

            FlowerAltsFirst = true;
            FlowerBeforeSpellMacros = true;
            FlowerHasMinimum = true;
            FlowerMinimumMana = 10000;

            AutoUpdateEnabled = true;

            // debug settings
            LoggingEnabled = false;
            ShowAllProcesses = false;
        }

        private static string FindExecutablePath()
        {
            var possiblePaths = new string[] {
                Path.Combine("C:", "Dark Ages", "DarkAges.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KRU", "Dark Ages", "Darkages.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "KRU", "Dark Ages", "Darkages.exe")
            };

            foreach (var possiblePath in possiblePaths)
            {
                if (File.Exists(possiblePath))
                    return possiblePath;
            }

            return possiblePaths.Last();
        }

        private static ObservationChangeAction ParseObservationChangeAction(
            string value,
            ObservationChangeAction fallback)
        {
            if (Enum.TryParse(
                    value,
                    ignoreCase: true,
                    out ObservationChangeAction action) &&
                Enum.IsDefined(action))
            {
                return action;
            }

            if (string.Equals(
                    value,
                    "ForceQuit",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ObservationChangeAction.CloseClient;
            }

            if (string.Equals(
                    value,
                    "None",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    value,
                    "Start",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    value,
                    "Resume",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    value,
                    "Restart",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ObservationChangeAction.Continue;
            }

            return fallback;
        }
    }
}
