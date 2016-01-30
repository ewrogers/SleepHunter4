using System;
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

using Microsoft.Win32;

using SleepHunter.Data;
using SleepHunter.Settings;

namespace SleepHunter
{
    public partial class SettingsWindow : Window
    {
        static readonly string DotNetRegistryKey = @"Software\Microsoft\NET Framework Setup\NDP\v4\Full\";

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
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var isDebug = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration == "Debug";
            versionText.Text = string.Format("Version {0}.{1}.{2}", version.Major.ToString(), version.Minor.ToString(), version.Build.ToString());

            var buildNumber = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var buildYear = Convert.ToInt32(buildNumber.Substring(0, 2)) + 2000;
            var buildMonth = Convert.ToInt32(buildNumber.Substring(2, 1), 16);
            var buildDay = Convert.ToInt32(buildNumber.Substring(3, 2));
            var buildIncrement = buildNumber.Substring(5, 1);

            var buildDate = new DateTime(buildYear, buildMonth, buildDay);

            buildText.Text = string.Format("Build {0}", buildNumber);
            buildDateText.Text = buildDate.ToString("MMMM dd, yyyy");

            if (isDebug)
                buildText.Text += "  (Debug)";

            var frameworkVersion = "???";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(DotNetRegistryKey))
            {
                var releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                if (releaseKey >= 393295)
                    frameworkVersion = "4.6+";
                else if (releaseKey >= 379893)
                    frameworkVersion = "4.5.2+";
                else if (releaseKey >= 378675)
                    frameworkVersion = "4.5.1+";
                else if (releaseKey >= 378389)
                    frameworkVersion = "4.5+";
            }

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

            var tabItem = tabControl.SelectedItem as TabItem;
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
