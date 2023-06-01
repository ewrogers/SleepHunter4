using System;
using System.IO;
using System.Runtime.InteropServices;
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
        public static readonly string CurrentVersion = "1.4";

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
        private bool directDrawFix = true;
        private bool allowMultipleInstances = true;
        private bool skipintroVideo = true;
        private bool noWalls = false;

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
            get { return isDebugMode; }
            set { SetProperty(ref isDebugMode, value); }
        }

        [XmlAttribute("Version")]
        public string Version
        {
            get { return version; }
            set { SetProperty(ref version, value); }
        }

        [XmlIgnore]
        public TimeSpan ProcessUpdateInterval
        {
            get { return processUpdateInterval; }
            set { SetProperty(ref processUpdateInterval, value, onChanged: (s) => { RaisePropertyChanged(nameof(ProcessUpdateIntervalSeconds)); }); }
        }

        [XmlElement("ProcessUpdateInterval")]
        public double ProcessUpdateIntervalSeconds
        {
            get { return processUpdateInterval.TotalSeconds; }
            set { ProcessUpdateInterval = TimeSpan.FromSeconds(value); }
        }

        [XmlIgnore]
        public TimeSpan ClientUpdateInterval
        {
            get { return clientUpdateInterval; }
            set { SetProperty(ref clientUpdateInterval, value, onChanged: (s) => { RaisePropertyChanged(nameof(ClientUpdateIntervalSeconds)); }); }
        }

        [XmlElement("ClientUpdateInterval")]
        public double ClientUpdateIntervalSeconds
        {
            get { return clientUpdateInterval.TotalSeconds; }
            set { ClientUpdateInterval = TimeSpan.FromSeconds(value); }
        }

        [XmlElement("SaveMacroStates")]
        public bool SaveMacroStates
        {
            get { return saveMacroStates; }
            set { SetProperty(ref saveMacroStates, value); }
        }

        [XmlElement("SelectedTheme")]
        public string SelectedTheme
        {
            get { return selectedTheme; }
            set { SetProperty(ref selectedTheme, value); }
        }

        [XmlElement("ClientSortOrder")]
        public PlayerSortOrder ClientSortOrder
        {
            get { return clientSortOrder; }
            set { SetProperty(ref clientSortOrder, value); }
        }

        [XmlElement("SkillIconSize")]
        public double SkillIconSize
        {
            get { return skillIconSize; }
            set { SetProperty(ref skillIconSize, value); }
        }

        [XmlElement("SkillGridWidth")]
        public int SkillGridWidth
        {
            get { return skillGridWidth; }
            set { SetProperty(ref skillGridWidth, value); }
        }

        [XmlElement("WorldSkillGridWidth")]
        public int WorldSkillGridWidth
        {
            get { return worldSkillGridWidth; }
            set { SetProperty(ref worldSkillGridWidth, value); }
        }

        [XmlElement("SpellGridWidth")]
        public int SpellGridWidth
        {
            get { return spellGridWidth; }
            set { SetProperty(ref spellGridWidth, value); }
        }

        [XmlElement("WorldSpellGridWidth")]
        public int WorldSpellGridWidth
        {
            get { return worldSpellGridWidth; }
            set { SetProperty(ref worldSpellGridWidth, value); }
        }

        [XmlElement("ShowSkillNames")]
        public bool ShowSkillNames
        {
            get { return showSkillNames; }
            set { SetProperty(ref showSkillNames, value); }
        }

        [XmlElement("ShowSkillLevels")]
        public bool ShowSkillLevels
        {
            get { return showSkillLevels; }
            set { SetProperty(ref showSkillLevels, value); }
        }

        [XmlElement("ShowSpellNames")]
        public bool ShowSpellNames
        {
            get { return showSpellNames; }
            set { SetProperty(ref showSpellNames, value); }
        }

        [XmlElement("ShowSpellLevels")]
        public bool ShowSpellLevels
        {
            get { return showSpellLevels; }
            set { SetProperty(ref showSpellLevels, value); }
        }

        [XmlElement("ClientPath")]
        public string ClientPath
        {
            get { return clientPath; }
            set { SetProperty(ref clientPath, value); }
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
            get { return iconDataFile; }
            set { SetProperty(ref iconDataFile, value, onChanged: (s) => { RaisePropertyChanged("IconDataFilePath"); }); }
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
            get { return paletteDataFile; }
            set { SetProperty(ref paletteDataFile, value, onChanged: (s) => { RaisePropertyChanged("PaletteDataFilePath"); }); }
        }

        [XmlElement("SkillIconFile")]
        public string SkillIconFile
        {
            get { return skillIconFile; }
            set { SetProperty(ref skillIconFile, value); }
        }

        [XmlElement("SkillPaletteFile")]
        public string SkillPaletteFile
        {
            get { return skillPaletteFile; }
            set { SetProperty(ref skillPaletteFile, value); }
        }

        [XmlElement("SpellIconFile")]
        public string SpellIconFile
        {
            get { return spellIconFile; }
            set { SetProperty(ref spellIconFile, value); }
        }

        [XmlElement("SpellPaletteFile")]
        public string SpellPaletteFile
        {
            get { return spellPaletteFile; }
            set { SetProperty(ref spellPaletteFile, value); }
        }

        [XmlElement("SelectedVersion")]
        public string SelectedVersion
        {
            get { return selectedVersion; }
            set { SetProperty(ref selectedVersion, value); }
        }

        [XmlElement("DirectDrawFix")]
        public bool DirectDrawFix
        {
            get { return directDrawFix; }
            set { SetProperty(ref directDrawFix, value); }
        }

        [XmlElement("AllowMultipleInstances")]
        public bool AllowMultipleInstances
        {
            get { return allowMultipleInstances; }
            set { SetProperty(ref allowMultipleInstances, value); }
        }

        [XmlElement("SkipIntroVideo")]
        public bool SkipIntroVideo
        {
            get { return skipintroVideo; }
            set { SetProperty(ref skipintroVideo, value); }
        }

        [XmlElement("NoWalls")]
        public bool NoWalls
        {
            get { return noWalls; }
            set { SetProperty(ref noWalls, value); }
        }

        [XmlElement("MapChangeAction")]
        public MacroAction MapChangeAction
        {
            get { return mapChangeAction; }
            set { SetProperty(ref mapChangeAction, value); }
        }

        [XmlElement("CoordsChangeAction")]
        public MacroAction CoordsChangeAction
        {
            get { return coordsChangeAction; }
            set { SetProperty(ref coordsChangeAction, value); }
        }

        [XmlElement("UseShiftForMedeniaPane")]
        public bool UseShiftForMedeniaPane
        {
            get { return useShiftForMedeniaPane; }
            set { SetProperty(ref useShiftForMedeniaPane, value); }
        }

        [XmlElement("PreserveUserPanel")]
        public bool PreserveUserPanel
        {
            get { return preserveUserPanel; }
            set { SetProperty(ref preserveUserPanel, value); }
        }

        [XmlElement("UseSpaceForAssail")]
        public bool UseSpaceForAssail
        {
            get { return useSpaceForAssail; }
            set { SetProperty(ref useSpaceForAssail, value); }
        }

        [XmlElement("DisarmForAssails")]
        public bool DisarmForAssails
        {
            get { return disarmForAssails; }
            set { SetProperty(ref disarmForAssails, value); }
        }

        [XmlElement("SpellRotationMode")]
        public SpellRotationMode SpellRotationMode
        {
            get { return spellRotationMode; }
            set { SetProperty(ref spellRotationMode, value); }
        }

        [XmlIgnore]
        public TimeSpan ZeroLineDelay
        {
            get { return zeroLineDelay; }
            set { SetProperty(ref zeroLineDelay, value, onChanged: (s) => { RaisePropertyChanged("ZeroLineDelaySeconds"); }); }
        }

        [XmlElement("ZeroLineDelay")]
        public double ZeroLineDelaySeconds
        {
            get { return zeroLineDelay.TotalSeconds; }
            set { ZeroLineDelay = TimeSpan.FromSeconds(value); }
        }

        [XmlIgnore]
        public TimeSpan SingleLineDelay
        {
            get { return singleLineDelay; }
            set { SetProperty(ref singleLineDelay, value, onChanged: (s) => { RaisePropertyChanged("SingleLineDelaySeconds"); }); }
        }

        [XmlElement("SingleLineDelay")]
        public double SingleLineDelaySeconds
        {
            get { return singleLineDelay.TotalSeconds; }
            set { SingleLineDelay = TimeSpan.FromSeconds(value); }
        }

        [XmlIgnore]
        public TimeSpan MultipleLineDelay
        {
            get { return multipleLineDelay; }
            set { SetProperty(ref multipleLineDelay, value, onChanged: (s) => { RaisePropertyChanged("MultipleLineDelaySeconds"); }); }
        }

        [XmlElement("MultipleLineDelay")]
        public double MultipleLineDelaySeconds
        {
            get { return multipleLineDelay.TotalSeconds; }
            set { MultipleLineDelay = TimeSpan.FromSeconds(value); }
        }

        [XmlElement("UseFasSpiorad")]
        public bool UseFasSpiorad
        {
            get { return useFasSpiorad; }
            set { SetProperty(ref useFasSpiorad, value); }
        }

        [XmlElement("UseFasSpioradOnDemand")]
        public bool UseFasSpioradOnDemand
        {
            get { return useFasSpioradOnDemand; }
            set { SetProperty(ref useFasSpioradOnDemand, value); }
        }

        [XmlElement("FasSpioradThreshold")]
        public double FasSpioradThreshold
        {
            get { return fasSpioradThreshold; }
            set { SetProperty(ref fasSpioradThreshold, value); }
        }

        [XmlElement("RequireManaForSpells")]
        public bool RequireManaForSpells
        {
            get { return requireManaForSpells; }
            set { SetProperty(ref requireManaForSpells, value); }
        }

        [XmlElement("AllowStaffSwitching")]
        public bool AllowStaffSwitching
        {
            get { return allowStaffSwitching; }
            set { SetProperty(ref allowStaffSwitching, value); }
        }

        [XmlElement("WarnOnDuplicateSpells")]
        public bool WarnOnDuplicateSpells
        {
            get { return warnOnDuplicateSpells; }
            set { SetProperty(ref warnOnDuplicateSpells, value); }
        }

        [XmlElement("FlowerAltsFirst")]
        public bool FlowerAltsFirst
        {
            get { return flowerAltsFirst; }
            set { SetProperty(ref flowerAltsFirst, value); }
        }

        [XmlElement("FlowerBeforeSpellMacros")]
        public bool FlowerBeforeSpellMacros
        {
            get { return flowerBeforeSpellMacros; }
            set { SetProperty(ref flowerBeforeSpellMacros, value); }
        }

        [XmlElement("FlowerHasMinimum")]
        public bool FlowerHasMinimum
        {
            get { return flowerHasMinimum; }
            set { SetProperty(ref flowerHasMinimum, value); }
        }

        [XmlElement("FlowerMinimumMana")]
        public int FlowerMinimumMana
        {
            get { return flowerMinimumMana; }
            set { SetProperty(ref flowerMinimumMana, value); }
        }

        [XmlElement("CheckForUpdates")]
        public bool AutoUpdateEnabled
        {
            get { return autoUpdateEnabled; }
            set { SetProperty(ref autoUpdateEnabled, value); }
        }

        [XmlElement("LoggingEnabled")]
        public bool LoggingEnabled
        {
            get { return loggingEnabled; }
            set { SetProperty(ref loggingEnabled, value); }
        }

        [XmlElement("ShowAllProcesses")]
        public bool ShowAllProcesses
        {
            get { return showAllProcesses; }
            set { SetProperty(ref showAllProcesses, value); }
        }

        public UserSettings()
        {
            // The DirectDraw patch may not work properly on non-x86 operating systems, disable by default for ARM
            var osArch = RuntimeInformation.OSArchitecture;
            directDrawFix = osArch == Architecture.X86 || osArch == Architecture.X64;
        }

        public static UserSettings CreateDefaults()
        {
            var settings = new UserSettings();
            settings.ResetDefaults();
            return settings;
        }

        public void ResetDefaults()
        {
            var osArch = RuntimeInformation.OSArchitecture;

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
            // The DirectDraw patch may not work properly on non-x86 operating systems, disable by default for ARM
            DirectDrawFix = osArch == Architecture.X86 || osArch == Architecture.X64;
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
