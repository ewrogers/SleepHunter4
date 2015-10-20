﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using SleepHunter.Data;
using SleepHunter.Settings;
using SleepHunter.Updates;

namespace SleepHunter
{
   public partial class SettingsWindow : Window
   {
      public static readonly int GeneralTabIndex = 0;
      public static readonly int UserInterfaceTabIndex = 1;
      public static readonly int GameClientTabIndex = 2;
      public static readonly int AllMacrosTabIndex = 3;
      public static readonly int SkillMacrosTabIndex = 4;
      public static readonly int SpellMacrosTabIndex = 5;
      public static readonly int FloweringTabIndex = 6;
      public static readonly int NotificationsTabIndex = 7;
      public static readonly int AboutTabIndex = 8;
      public static readonly int DebugTabIndex = 9;

      public int SelectedTabIndex
      {
         get { return (int)GetValue(SelectedTabIndexProperty); }
         set { SetValue(SelectedTabIndexProperty, value); }
      }

      public static readonly DependencyProperty SelectedTabIndexProperty =
          DependencyProperty.Register("SelectedTabIndex", typeof(int), typeof(SettingsWindow), new PropertyMetadata(0));

      public SettingsWindow()
      {
         InitializeComponent();
         GetVersion();
      }

      void GetVersion()
      {
         Version version;
         string build;
         bool isDebug;
         VersionExtender.GetCurrentVersionInfo(out version, out build, out isDebug);

         versionText.Text = string.Format("Version {0}.{1}.{2}", version.Major.ToString(), version.Minor.ToString(), version.Build.ToString());
         buildText.Text = string.Format("Build {0}", build);

         DateTime buildDate;
         int revision;

         if (!BuildHelper.TryGetBuildTime(build, out buildDate, out revision))
            buildDateText.Visibility = Visibility.Collapsed;
         else
            buildDateText.Text = string.Format("{0}", buildDate.ToLongDateString());

         if (isDebug)
            debugText.Visibility = Visibility.Visible;

         var assembly = Assembly.GetExecutingAssembly();
         var frameworkVersion = assembly.ImageRuntimeVersion;

         frameworkVersionText.Text = string.Format(".NET Framework {0}", frameworkVersion);
      }

      void resetDefaultsButton_Click(object sender, RoutedEventArgs e)
      {
         bool? isOkToReset = this.ShowMessageBox("Reset Default Settings",
            "This will reset all settings to their default values.\nDo you wish to continue?",
            "This action cannot be undone.",
            MessageBoxButton.YesNo,
            460, 240);

         if (isOkToReset.Value)
            UserSettingsManager.Instance.Settings.ResetDefaults();
      }

      void resetVersionsButton_Click(object sender, RoutedEventArgs e)
      {
         bool? isOkToReset = this.ShowMessageBox("Reset Client Versions",
            "This will reset client version data to the defaults.\nDo you wish to continue?",
            "This action cannot be undone.",
            MessageBoxButton.YesNo,
            460, 240);

         if (isOkToReset.Value)
         {
            ClientVersionManager.Instance.ClearVersions();
            ClientVersionManager.Instance.LoadDefaultVersions();

            clientVersionComboBox.ItemsSource = ClientVersionManager.Instance.Versions;
         }
      }

      void resetThemesButton_Click(object sender, RoutedEventArgs e)
      {
         bool? isOkToReset = this.ShowMessageBox("Reset Color Themes",
            "This will restore all the default color themes.\nDo you wish to continue?",
            "This action cannot be undone.",
            MessageBoxButton.YesNo,
            460, 240);

         if (isOkToReset.Value)
         {
            ColorThemeManager.Instance.ClearThemes();
            ColorThemeManager.Instance.LoadDefaultThemes();

            themeColorComboBox.ItemsSource = ColorThemeManager.Instance.Themes;
         }
      }

      void metadataEditorButton_Click(object sender, RoutedEventArgs e)
      {
         var mainWindow = this.Owner as MainWindow;
         if (mainWindow == null) return;

         mainWindow.ShowMetadataWindow();
      }

      void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         var tabControl = sender as TabControl;
         if (tabControl == null) return;

         var tabItem= tabControl.SelectedItem as TabItem;
         if (tabItem == null)
         {
            this.Title = "Settings";
            return;
         }

         this.Title = string.Format("Settings - {0}", (tabItem.Header as string).Replace("_", string.Empty));
      }

      void updateButton_Click(object sender, RoutedEventArgs e)
      {
         var mainWindow = this.Owner as MainWindow;
         if (mainWindow == null) return;

         mainWindow.CheckForUpdate();
      }
   }
}
