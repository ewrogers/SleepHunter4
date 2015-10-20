using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using SleepHunter.Data;
using SleepHunter.Macro;

namespace SleepHunter.Settings
{
   [Serializable]
   [XmlRoot("UserSettings")]
   public class UserSettings : NotifyObject
   {
      public static readonly string CurrentVersion = "1.0";

      bool isDebugMode;

      // Regular Settings
      string version;
      TimeSpan processUpdateInterval;
      TimeSpan clientUpdateInterval;
      bool saveMacroStates;
      string selectedTheme;
      bool rainbowMode;
      double skillIconSize;
      int skillGridWidth;
      int worldSkillGridWidth;
      int spellGridWidth;
      int worldSpellGridWidth;
      bool showSkillNames;
      bool showSkillLevels;
      bool showSpellNames;
      bool showSpellLevels;
      string clientPath;
      string iconDataFile;
      string paletteDataFile;
      string skillIconFile;
      string skillPaletteFile;
      string spellIconFile;
      string spellPaletteFile;
      string selectedVersion;
      bool allowMultipleInstances;
      bool skipintroVideo;
      bool noWalls;
      MacroAction mapChangeAction;
      MacroAction coordsChangeAction;
      bool useShiftForMedeniaPane;
      bool preserveUserPanel;
      bool useSpaceForAssail;
      bool disarmForAssails;
      SpellRotationMode spellRotationMode;
      TimeSpan zeroLineDelay;
      TimeSpan singleLineDelay;
      TimeSpan multipleLineDelay;
      bool useFasSpiorad;
      bool useFasSpioradOnDemand;
      double fasSpioradThreshold;
      bool requireManaForSpells;
      bool allowStaffSwitching;
      bool warnOnDuplicateSpells;
      bool flowerAltsFirst;
      bool flowerBeforeSpellMacros;
      bool flowerHasMinimum;
      int flowerMinimumMana;
      bool autoUpdateEnabled;

      [XmlIgnore]
      public bool IsDebugMode
      {
         get { return isDebugMode; }
         set { SetProperty(ref isDebugMode, value, "IsDebugMode"); }
      }

      [XmlAttribute("Version")]
      public string Version
      {
         get { return version; }
         set { SetProperty(ref version, value, "Version"); }
      }

      [XmlIgnore]
      public TimeSpan ProcessUpdateInterval
      {
         get { return processUpdateInterval; }
         set { SetProperty(ref processUpdateInterval, value, "ProcessUpdateInterval", onChanged: (s) => { OnPropertyChanged("ProcessUpdateIntervalSeconds"); }); }
      }

      [XmlElement("ProcessUpdateInterval")]
      public double ProcessUpdateIntervalSeconds
      {
         get { return processUpdateInterval.TotalSeconds; }
         set { this.ProcessUpdateInterval = TimeSpan.FromSeconds(value); }
      }

      [XmlIgnore]
      public TimeSpan ClientUpdateInterval
      {
         get { return clientUpdateInterval; }
         set { SetProperty(ref clientUpdateInterval, value, "ClientUpdateInterval", onChanged: (s) => { OnPropertyChanged("ClientUpdateIntervalSeconds"); }); }
      }

      [XmlElement("ClientUpdateInterval")]
      public double ClientUpdateIntervalSeconds
      {
         get { return clientUpdateInterval.TotalSeconds; }
         set { this.ClientUpdateInterval = TimeSpan.FromSeconds(value); }
      }


      [XmlElement("SaveMacroStates")]
      public bool SaveMacroStates
      {
         get { return saveMacroStates; }
         set { SetProperty(ref saveMacroStates, value, "SaveMacroStates"); }
      }

      [XmlElement("SelectedTheme")]
      public string SelectedTheme
      {
         get { return selectedTheme; }
         set { SetProperty(ref selectedTheme, value, "SelectedTheme"); }
      }

      [XmlElement("RainbowMode")]
      public bool RainbowMode
      {
         get { return rainbowMode; }
         set { SetProperty(ref rainbowMode, value, "RainbowMode"); }
      }

      [XmlElement("SkillIconSize")]
      public double SkillIconSize
      {
         get{return skillIconSize;}
         set { SetProperty(ref skillIconSize, value, "SkillIconSize"); }
      }

      [XmlElement("SkillGridWidth")]
      public int SkillGridWidth
      {
         get { return skillGridWidth; }
         set { SetProperty(ref skillGridWidth, value, "SkillGridWidth"); }
      }

      [XmlElement("WorldSkillGridWidth")]
      public int WorldSkillGridWidth
      {
         get { return worldSkillGridWidth; }
         set { SetProperty(ref worldSkillGridWidth, value, "WorldSkillGridWidth"); }
      }

      [XmlElement("SpellGridWidth")]
      public int SpellGridWidth
      {
         get { return spellGridWidth; }
         set { SetProperty(ref spellGridWidth, value, "SpellGridWidth"); }
      }

      [XmlElement("WorldSpellGridWidth")]
      public int WorldSpellGridWidth
      {
         get { return worldSpellGridWidth; }
         set { SetProperty(ref worldSpellGridWidth, value, "WorldSpellGridWidth"); }
      }

      [XmlElement("ShowSkillNames")]
      public bool ShowSkillNames
      {
         get { return showSkillNames; }
         set { SetProperty(ref showSkillNames, value, "ShowSkillNames"); }
      }

      [XmlElement("ShowSkillLevels")]
      public bool ShowSkillLevels
      {
         get { return showSkillLevels; }
         set { SetProperty(ref showSkillLevels, value, "ShowSkillLevels"); }
      }

      [XmlElement("ShowSpellNames")]
      public bool ShowSpellNames
      {
         get { return showSpellNames; }
         set { SetProperty(ref showSpellNames, value, "ShowSpellNames"); }
      }

      [XmlElement("ShowSpellLevels")]
      public bool ShowSpellLevels
      {
         get { return showSpellLevels; }
         set { SetProperty(ref showSpellLevels, value, "ShowSpellLevels"); }
      }

      [XmlElement("ClientPath")]
      public string ClientPath
      {
         get { return clientPath; }
         set { SetProperty(ref clientPath, value, "ClientPath"); }
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
         set { SetProperty(ref iconDataFile, value, "IconDataFile", onChanged: (s) => { OnPropertyChanged("IconDataFilePath"); }); }
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
         set { SetProperty(ref paletteDataFile, value, "PaletteDataFile", onChanged: (s) => { OnPropertyChanged("PaletteDataFilePath"); }); }
      }

      [XmlElement("SkillIconFile")]
      public string SkillIconFile
      {
         get { return skillIconFile; }
         set { SetProperty(ref skillIconFile, value, "SkillIconFile"); }
      }

      [XmlElement("SkillPaletteFile")]
      public string SkillPaletteFile
      {
         get { return skillPaletteFile; }
         set { SetProperty(ref skillPaletteFile, value, "SkillPaletteFile"); }
      }

      [XmlElement("SpellIconFile")]
      public string SpellIconFile
      {
         get { return spellIconFile; }
         set { SetProperty(ref spellIconFile, value, "SpellIconFile"); }
      }

      [XmlElement("SpellPaletteFile")]
      public string SpellPaletteFile
      {
         get { return spellPaletteFile; }
         set { SetProperty(ref spellPaletteFile, value, "SpellPaletteFile"); }
      }

      [XmlElement("SelectedVersion")]
      public string SelectedVersion
      {
         get { return selectedVersion; }
         set { SetProperty(ref selectedVersion, value, "SelectedVersion"); }
      }

      [XmlElement("AllowMultipleInstances")]
      public bool AllowMultipleInstances
      {
         get { return allowMultipleInstances; }
         set { SetProperty(ref allowMultipleInstances, value, "AllowMultipleInstances"); }
      }

      [XmlElement("SkipIntroVideo")]
      public bool SkipIntroVideo
      {
         get { return skipintroVideo; }
         set { SetProperty(ref skipintroVideo, value, "SkipIntroVideo"); }
      }

      [XmlElement("NoWalls")]
      public bool NoWalls
      {
         get { return noWalls; }
         set { SetProperty(ref noWalls, value, "NoWalls"); }
      }

      [XmlElement("MapChangeAction")]
      public MacroAction MapChangeAction
      {
         get { return mapChangeAction; }
         set { SetProperty(ref mapChangeAction, value, "MapChangeAction"); }
      }

      [XmlElement("CoordsChangeAction")]
      public MacroAction CoordsChangeAction
      {
         get { return coordsChangeAction; }
         set { SetProperty(ref coordsChangeAction, value, "CoordsChangeAction"); }
      }

      [XmlElement("UseShiftForMedeniaPane")]
      public bool UseShiftForMedeniaPane
      {
         get { return useShiftForMedeniaPane; }
         set { SetProperty(ref useShiftForMedeniaPane, value, "UseShiftForMedeniaPane"); }
      }

      [XmlElement("PreserveUserPanel")]
      public bool PreserveUserPanel
      {
         get { return preserveUserPanel; }
         set { SetProperty(ref preserveUserPanel, value, "PreserveUserPanel"); }
      }

      [XmlElement("UseSpaceForAssail")]
      public bool UseSpaceForAssail
      {
         get { return useSpaceForAssail; }
         set { SetProperty(ref useSpaceForAssail, value, "UseSpaceForAssail"); }
      }

      [XmlElement("DisarmForAssails")]
      public bool DisarmForAssails
      {
         get { return disarmForAssails; }
         set { SetProperty(ref disarmForAssails, value, "DisarmForAssails"); }
      }

      [XmlElement("SpellRotationMode")]
      public SpellRotationMode SpellRotationMode
      {
         get { return spellRotationMode; }
         set { SetProperty(ref spellRotationMode, value, "SpellRotationMode"); }
      }

      [XmlIgnore]
      public TimeSpan ZeroLineDelay
      {
         get { return zeroLineDelay; }
         set { SetProperty(ref zeroLineDelay, value, "ZeroLineDelay", onChanged: (s) => { OnPropertyChanged("ZeroLineDelaySeconds"); }); }
      }

      [XmlElement("ZeroLineDelay")]
      public double ZeroLineDelaySeconds
      {
         get { return zeroLineDelay.TotalSeconds; }
         set { this.ZeroLineDelay = TimeSpan.FromSeconds(value); }
      }

      [XmlIgnore]
      public TimeSpan SingleLineDelay
      {
         get { return singleLineDelay; }
         set { SetProperty(ref singleLineDelay, value, "SingleLineDelay", onChanged: (s) => { OnPropertyChanged("SingleLineDelaySeconds"); }); }
      }

      [XmlElement("SingleLineDelay")]
      public double SingleLineDelaySeconds
      {
         get { return singleLineDelay.TotalSeconds; }
         set { this.SingleLineDelay = TimeSpan.FromSeconds(value); }
      }

      [XmlIgnore]
      public TimeSpan MultipleLineDelay
      {
         get { return multipleLineDelay; }
         set { SetProperty(ref multipleLineDelay, value, "MultipleLineDelay", onChanged: (s) => { OnPropertyChanged("MultipleLineDelaySeconds"); }); }
      }

      [XmlElement("MultipleLineDelay")]
      public double MultipleLineDelaySeconds
      {
         get { return multipleLineDelay.TotalSeconds; }
         set { this.MultipleLineDelay = TimeSpan.FromSeconds(value); }
      }

      [XmlElement("UseFasSpiorad")]
      public bool UseFasSpiorad
      {
         get { return useFasSpiorad; }
         set { SetProperty(ref useFasSpiorad, value, "UseFasSpiorad"); }
      }

      [XmlElement("UseFasSpioradOnDemand")]
      public bool UseFasSpioradOnDemand
      {
         get { return useFasSpioradOnDemand; }
         set { SetProperty(ref useFasSpioradOnDemand, value, "UseFasSpioradOnDemand"); }
      }

      [XmlElement("FasSpioradThreshold")]
      public double FasSpioradThreshold
      {
         get { return fasSpioradThreshold; }
         set { SetProperty(ref fasSpioradThreshold, value, "FasSpioradThreshold"); }
      }

      [XmlElement("RequireManaForSpells")]
      public bool RequireManaForSpells
      {
         get { return requireManaForSpells; }
         set { SetProperty(ref requireManaForSpells, value, "RequireManaForSpells"); }
      }

      [XmlElement("AllowStaffSwitching")]
      public bool AllowStaffSwitching
      {
         get { return allowStaffSwitching; }
         set { SetProperty(ref allowStaffSwitching, value, "AllowStaffSwitching"); }
      }

      [XmlElement("WarnOnDuplicateSpells")]
      public bool WarnOnDuplicateSpells
      {
         get { return warnOnDuplicateSpells; }
         set { SetProperty(ref warnOnDuplicateSpells, value, "WarnOnDuplicateSpells"); }
      }

      [XmlElement("FlowerAltsFirst")]
      public bool FlowerAltsFirst
      {
         get { return flowerAltsFirst; }
         set { SetProperty(ref flowerAltsFirst, value, "FlowerAltsFirst"); }
      }

      [XmlElement("FlowerBeforeSpellMacros")]
      public bool FlowerBeforeSpellMacros
      {
         get { return flowerBeforeSpellMacros; }
         set { SetProperty(ref flowerBeforeSpellMacros, value, "FlowerBeforeSpellMacros"); }
      }

      [XmlElement("FlowerHasMinimum")]
      public bool FlowerHasMinimum
      {
         get { return flowerHasMinimum; }
         set { SetProperty(ref flowerHasMinimum, value, "FlowerHasMinimum"); }
      }

      [XmlElement("FlowerMinimumMana")]
      public int FlowerMinimumMana
      {
         get { return flowerMinimumMana; }
         set { SetProperty(ref flowerMinimumMana, value, "FlowerMinimumMana"); }
      }

      [XmlElement("CheckForUpdates")]
      public bool AutoUpdateEnabled
      {
         get { return autoUpdateEnabled; }
         set { SetProperty(ref autoUpdateEnabled, value, "AutoUpdateEnabled"); }
      }

      public UserSettings()
      {
         ResetDefaults();
      }

      public void ResetDefaults()
      {
         this.Version = CurrentVersion;

         this.ProcessUpdateInterval = TimeSpan.FromSeconds(1);
         this.ClientUpdateInterval = TimeSpan.FromSeconds(0.2);
         this.SaveMacroStates = true;

         this.SelectedTheme = "Default";
         this.RainbowMode = false;
         this.SkillIconSize = 46;
         this.SkillGridWidth = 12;
         this.WorldSkillGridWidth = 6;
         this.SpellGridWidth = 12;
         this.WorldSpellGridWidth = 6;
         this.ShowSkillNames = false;
         this.ShowSkillLevels = true;
         this.ShowSpellNames = false;
         this.ShowSpellLevels = true;

         this.ClientPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "KRU", "Dark Ages", "Darkages.exe");
         this.IconDataFile = "setoa.dat";
         this.PaletteDataFile = "setoa.dat";
         this.SkillIconFile = "skill001.epf";
         this.SkillPaletteFile = "gui06.pal";
         this.SpellIconFile = "spell001.epf";
         this.SpellPaletteFile = "gui06.pal";

         this.SelectedVersion = "Auto-Detect";
         this.AllowMultipleInstances = true;
         this.SkipIntroVideo = true;
         this.NoWalls = false;

         this.MapChangeAction = MacroAction.Stop;
         this.CoordsChangeAction = MacroAction.None;
         this.UseShiftForMedeniaPane = true;
         this.PreserveUserPanel = true;

         this.UseSpaceForAssail = true;
         this.DisarmForAssails = true;

         this.SpellRotationMode = SpellRotationMode.RoundRobin;
         this.ZeroLineDelay = TimeSpan.FromSeconds(0.2);
         this.SingleLineDelay = TimeSpan.FromSeconds(1);
         this.MultipleLineDelay = TimeSpan.FromSeconds(1);
         this.UseFasSpiorad = false;
         this.FasSpioradThreshold = 1000;
         this.UseFasSpioradOnDemand = true;
         this.RequireManaForSpells = true;
         this.AllowStaffSwitching = true;
         this.WarnOnDuplicateSpells = true;
         this.FlowerAltsFirst = true;
         this.FlowerBeforeSpellMacros = true;
         this.FlowerHasMinimum = true;
         this.FlowerMinimumMana = 10000;
         
         this.AutoUpdateEnabled = true;

         foreach (var theme in ColorThemeManager.Instance.Themes)
            if (theme.IsDefault)
               this.SelectedTheme = theme.Name;
      }
   }
}
