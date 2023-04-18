using System;
using System.IO;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.Macro;
using SleepHunter.Models;

namespace SleepHunter.Settings
{
    [Serializable]
    [XmlRoot("UserSettings")]
    public class UserSettings : ObservableObject
    {
        public const string CurrentVersion = "1.3";

        private bool isDebugMode;
        private string version;

        // Regular Settings
        private TimeSpan processUpdateInterval;
        private TimeSpan clientUpdateInterval;
        private bool saveMacroStates;
        private string selectedTheme;
        private PlayerSortOrder clientSortOrder = PlayerSortOrder.LoginTime;
        private double skillIconSize;
        private int skillGridWidth;
        private int worldSkillGridWidth;
        private int spellGridWidth;
        private int worldSpellGridWidth;
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
        private string selectedVersion;
        private bool allowMultipleInstances;
        private bool skipintroVideo;
        private bool noWalls;
        private MacroAction mapChangeAction;
        private MacroAction coordsChangeAction;
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

        [XmlAttribute(nameof(Version))]
        public string Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        [XmlIgnore]
        public TimeSpan ProcessUpdateInterval
        {
            get => processUpdateInterval;
            set => SetProperty(ref processUpdateInterval, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(ProcessUpdateIntervalSeconds));
            });
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
            set => SetProperty(ref clientUpdateInterval, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(ClientUpdateIntervalSeconds));
            });
        }

        [XmlElement("ClientUpdateInterval")]
        public double ClientUpdateIntervalSeconds
        {
            get => clientUpdateInterval.TotalSeconds;
            set => ClientUpdateInterval = TimeSpan.FromSeconds(value);
        }

        [XmlElement(nameof(SaveMacroStates))]
        public bool SaveMacroStates
        {
            get => saveMacroStates;
            set => SetProperty(ref saveMacroStates, value);
        }

        [XmlElement(nameof(SelectedTheme))]
        public string SelectedTheme
        {
            get => selectedTheme;
            set => SetProperty(ref selectedTheme, value);
        }

        [XmlElement(nameof(ClientSortOrder))]
        public PlayerSortOrder ClientSortOrder
        {
            get => clientSortOrder;
            set => SetProperty(ref clientSortOrder, value);
        }

        [XmlElement(nameof(SkillIconSize))]
        public double SkillIconSize
        {
            get => skillIconSize;
            set => SetProperty(ref skillIconSize, value);
        }

        [XmlElement(nameof(SkillGridWidth))]
        public int SkillGridWidth
        {
            get => skillGridWidth;
            set => SetProperty(ref skillGridWidth, value);
        }

        [XmlElement(nameof(WorldSkillGridWidth))]
        public int WorldSkillGridWidth
        {
            get => worldSkillGridWidth;
            set => SetProperty(ref worldSkillGridWidth, value);
        }

        [XmlElement(nameof(SpellGridWidth))]
        public int SpellGridWidth
        {
            get => spellGridWidth;
            set => SetProperty(ref spellGridWidth, value);
        }

        [XmlElement(nameof(WorldSpellGridWidth))]
        public int WorldSpellGridWidth
        {
            get => worldSpellGridWidth;
            set => SetProperty(ref worldSpellGridWidth, value);
        }

        [XmlElement(nameof(ShowSkillNames))]
        public bool ShowSkillNames
        {
            get => showSkillNames;
            set => SetProperty(ref showSkillNames, value);
        }

        [XmlElement(nameof(ShowSkillLevels))]
        public bool ShowSkillLevels
        {
            get => showSkillLevels;
            set => SetProperty(ref showSkillLevels, value);
        }

        [XmlElement(nameof(ShowSpellNames))]
        public bool ShowSpellNames
        {
            get => showSpellNames;
            set => SetProperty(ref showSpellNames, value);
        }

        [XmlElement(nameof(ShowSpellLevels))]
        public bool ShowSpellLevels
        {
            get => showSpellLevels;
            set => SetProperty(ref showSpellLevels, value);
        }

        [XmlElement(nameof(ClientPath))]
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

        [XmlElement(nameof(IconDataFile))]
        public string IconDataFile
        {
            get => iconDataFile;
            set => SetProperty(ref iconDataFile, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(IconDataFilePath));
           });
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

        [XmlElement(nameof(PaletteDataFile))]
        public string PaletteDataFile
        {
            get => paletteDataFile;
            set => SetProperty(ref paletteDataFile, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(PaletteDataFilePath));
            });
        }

        [XmlElement(nameof(SkillIconFile))]
        public string SkillIconFile
        {
            get => skillIconFile;
            set => SetProperty(ref skillIconFile, value);
        }

        [XmlElement(nameof(SkillPaletteFile))]
        public string SkillPaletteFile
        {
            get => skillPaletteFile;
            set => SetProperty(ref skillPaletteFile, value);
        }

        [XmlElement(nameof(SpellIconFile))]
        public string SpellIconFile
        {
            get => spellIconFile;
            set => SetProperty(ref spellIconFile, value);
        }

        [XmlElement(nameof(SpellPaletteFile))]
        public string SpellPaletteFile
        {
            get => spellPaletteFile;
            set => SetProperty(ref spellPaletteFile, value);
        }

        [XmlElement(nameof(SelectedVersion))]
        public string SelectedVersion
        {
            get => selectedVersion;
            set => SetProperty(ref selectedVersion, value);
        }

        [XmlElement(nameof(AllowMultipleInstances))]
        public bool AllowMultipleInstances
        {
            get => allowMultipleInstances;
            set => SetProperty(ref allowMultipleInstances, value);
        }

        [XmlElement(nameof(SkipIntroVideo))]
        public bool SkipIntroVideo
        {
            get => skipintroVideo;
            set => SetProperty(ref skipintroVideo, value);
        }

        [XmlElement(nameof(NoWalls))]
        public bool NoWalls
        {
            get => noWalls;
            set => SetProperty(ref noWalls, value);
        }

        [XmlElement(nameof(MapChangeAction))]
        public MacroAction MapChangeAction
        {
            get => mapChangeAction;
            set => SetProperty(ref mapChangeAction, value);
        }

        [XmlElement(nameof(CoordsChangeAction))]
        public MacroAction CoordsChangeAction
        {
            get => coordsChangeAction;
            set => SetProperty(ref coordsChangeAction, value);
        }

        [XmlElement(nameof(UseShiftForMedeniaPane))]
        public bool UseShiftForMedeniaPane
        {
            get => useShiftForMedeniaPane;
            set => SetProperty(ref useShiftForMedeniaPane, value);
        }

        [XmlElement(nameof(PreserveUserPanel))]
        public bool PreserveUserPanel
        {
            get => preserveUserPanel;
            set => SetProperty(ref preserveUserPanel, value);
        }

        [XmlElement(nameof(UseSpaceForAssail))]
        public bool UseSpaceForAssail
        {
            get => useSpaceForAssail;
            set => SetProperty(ref useSpaceForAssail, value);
        }

        [XmlElement(nameof(DisarmForAssails))]
        public bool DisarmForAssails
        {
            get => disarmForAssails;
            set => SetProperty(ref disarmForAssails, value);
        }

        [XmlElement(nameof(SpellRotationMode))]
        public SpellRotationMode SpellRotationMode
        {
            get => spellRotationMode;
            set => SetProperty(ref spellRotationMode, value);
        }

        [XmlIgnore]
        public TimeSpan ZeroLineDelay
        {
            get => zeroLineDelay;
            set => SetProperty(ref zeroLineDelay, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(ZeroLineDelaySeconds));
            });
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
            set => SetProperty(ref singleLineDelay, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(SingleLineDelaySeconds));
            });
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
            set => SetProperty(ref multipleLineDelay, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(MultipleLineDelaySeconds));
            });
        }

        [XmlElement("MultipleLineDelay")]
        public double MultipleLineDelaySeconds
        {
            get => multipleLineDelay.TotalSeconds;
            set => MultipleLineDelay = TimeSpan.FromSeconds(value);
        }

        [XmlElement(nameof(UseFasSpiorad))]
        public bool UseFasSpiorad
        {
            get => useFasSpiorad;
            set => SetProperty(ref useFasSpiorad, value);
        }

        [XmlElement(nameof(UseFasSpioradOnDemand))]
        public bool UseFasSpioradOnDemand
        {
            get => useFasSpioradOnDemand;
            set => SetProperty(ref useFasSpioradOnDemand, value);
        }

        [XmlElement(nameof(FasSpioradThreshold))]
        public double FasSpioradThreshold
        {
            get => fasSpioradThreshold;
            set => SetProperty(ref fasSpioradThreshold, value);
        }

        [XmlElement(nameof(RequireManaForSpells))]
        public bool RequireManaForSpells
        {
            get => requireManaForSpells;
            set => SetProperty(ref requireManaForSpells, value);
        }

        [XmlElement(nameof(AllowStaffSwitching))]
        public bool AllowStaffSwitching
        {
            get => allowStaffSwitching;
            set => SetProperty(ref allowStaffSwitching, value);
        }

        [XmlElement(nameof(WarnOnDuplicateSpells))]
        public bool WarnOnDuplicateSpells
        {
            get => warnOnDuplicateSpells;
            set => SetProperty(ref warnOnDuplicateSpells, value);
        }

        [XmlElement(nameof(FlowerAltsFirst))]
        public bool FlowerAltsFirst
        {
            get => flowerAltsFirst;
            set => SetProperty(ref flowerAltsFirst, value);
        }

        [XmlElement(nameof(FlowerBeforeSpellMacros))]
        public bool FlowerBeforeSpellMacros
        {
            get => flowerBeforeSpellMacros;
            set => SetProperty(ref flowerBeforeSpellMacros, value);
        }

        [XmlElement(nameof(FlowerHasMinimum))]
        public bool FlowerHasMinimum
        {
            get => flowerHasMinimum;
            set => SetProperty(ref flowerHasMinimum, value);
        }

        [XmlElement(nameof(FlowerMinimumMana))]
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

        [XmlElement(nameof(LoggingEnabled))]
        public bool LoggingEnabled
        {
            get => loggingEnabled;
            set => SetProperty(ref loggingEnabled, value);
        }

        [XmlElement(nameof(ShowAllProcesses))]
        public bool ShowAllProcesses
        {
            get => showAllProcesses;
            set => SetProperty(ref showAllProcesses, value);
        }

        public UserSettings() { }

        public static UserSettings CreateDefaults()
        {
            var settings = new UserSettings();
            settings.ResetDefaults();
            return settings;
        }

        public void ResetDefaults()
        {
            Version = CurrentVersion;

            ProcessUpdateInterval = TimeSpan.FromSeconds(1);
            ClientUpdateInterval = TimeSpan.FromSeconds(0.2);
            SaveMacroStates = true;

            SelectedTheme = ColorThemeManager.Instance.DefaultTheme?.Name ?? "Default";
            clientSortOrder = PlayerSortOrder.LoginTime;
            SkillIconSize = 46;
            SkillGridWidth = 12;
            WorldSkillGridWidth = 6;
            SpellGridWidth = 12;
            WorldSpellGridWidth = 6;
            ShowSkillNames = false;
            ShowSkillLevels = true;
            ShowSpellNames = false;
            ShowSpellLevels = true;

            ClientPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "KRU", "Dark Ages", "Darkages.exe");
            IconDataFile = "setoa.dat";
            PaletteDataFile = "setoa.dat";
            SkillIconFile = "skill001.epf";
            SkillPaletteFile = "gui06.pal";
            SpellIconFile = "spell001.epf";
            SpellPaletteFile = "gui06.pal";

            SelectedVersion = "Auto-Detect";
            AllowMultipleInstances = true;
            SkipIntroVideo = true;
            NoWalls = false;

            MapChangeAction = MacroAction.Stop;
            CoordsChangeAction = MacroAction.None;
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
            FlowerAltsFirst = true;
            FlowerBeforeSpellMacros = true;
            FlowerHasMinimum = true;
            FlowerMinimumMana = 10000;

            AutoUpdateEnabled = true;

            // debug settings
            LoggingEnabled = false;
            ShowAllProcesses = false;
        }
    }
}
