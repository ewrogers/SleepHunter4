using System;
using System.Windows;

using SleepHunter.Metadata;

namespace SleepHunter.Views
{
    public partial class StaffEditorWindow : Window
   {
      string originalName;
      StaffMetadata staff = new StaffMetadata();

      public StaffMetadata Staff
      {
         get { return staff; }
         private set { staff = value; }
      }

      public bool IsEditMode
      {
         get { return (bool)GetValue(IsEditModeProperty); }
         set { SetValue(IsEditModeProperty, value); }
      }

      public static readonly DependencyProperty IsEditModeProperty =
          DependencyProperty.Register("IsEditMode", typeof(bool), typeof(StaffEditorWindow), new PropertyMetadata(false));

      public StaffEditorWindow(StaffMetadata staff, bool isEditMode = true)
         :this()
      {
         nameTextBox.Text = originalName = staff.Name;

         if (staff.AbilityLevel > 0)
         {
            levelUpDown.Value = staff.AbilityLevel;
            isAbilityLevelCheckBox.IsChecked = true;
         }
         else
         {
            levelUpDown.Value = staff.Level;
            isAbilityLevelCheckBox.IsChecked = false;
         }

         SetPlayerClass(staff.Class);

         this.IsEditMode = isEditMode;

         if (isEditMode)
            this.Title = "Edit Staff";
      }

      public StaffEditorWindow()
      {
         InitializeComponent();

         this.Title = "Add Staff";
      }

      void Window_Loaded(object sender, RoutedEventArgs e)
      {
         nameTextBox.Focus();
         nameTextBox.SelectAll();
      }

      bool ValidateStaff()
      {
         string staffName = nameTextBox.Text.Trim();
         int level = (int)levelUpDown.Value;
         bool isMedenia = isAbilityLevelCheckBox.IsChecked.Value;
         bool nameChanged = originalName == null || !string.Equals(originalName, staffName, StringComparison.OrdinalIgnoreCase);

         if (string.IsNullOrWhiteSpace(staffName))
         {
            this.ShowMessageBox("Invalid Name",
               "Staff names must not be null or empty.",
               "This includes whitespace characters.",
               MessageBoxButton.OK,
               420, 220);

            nameTextBox.Focus();
            nameTextBox.SelectAll();
            return false;
         }

         if (nameChanged && StaffMetadataManager.Instance.ContainsStaff(staffName))
         {
            this.ShowMessageBox("Duplicate Name",
               "A staff already exists with the same name.",
               "Skill names are case-insenstive.",
               MessageBoxButton.OK,
               420, 220);

            nameTextBox.Focus();
            nameTextBox.SelectAll();
            return false;
         }

         staff.Name = staffName;
         staff.Level = isMedenia ? 0 : level;
         staff.AbilityLevel = isMedenia ? level : 0;
         staff.Class = GetPlayerClass();
         return true;
      }

      PlayerClass GetPlayerClass()
      {
         var playerClass = PlayerClass.Peasant;

         if (warriorCheckBox.IsChecked.Value)
            playerClass |= PlayerClass.Warrior;

         if (wizardCheckBox.IsChecked.Value)
            playerClass |= PlayerClass.Wizard;

         if (priestCheckBox.IsChecked.Value)
            playerClass |= PlayerClass.Priest;

         if (rogueCheckBox.IsChecked.Value)
            playerClass |= PlayerClass.Rogue;

         if (monkCheckBox.IsChecked.Value)
            playerClass |= PlayerClass.Monk;

         return playerClass;
      }

      void SetPlayerClass(PlayerClass playerClass)
      {
         warriorCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Warrior);
         wizardCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Wizard);
         priestCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Priest);
         rogueCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Rogue);
         monkCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Monk);
      }

      void okButton_Click(object sender, RoutedEventArgs e)
      {
         if (!ValidateStaff())
            return;

         this.DialogResult = true;
         this.Close();
      }

      void cancelButton_Click(object sender, RoutedEventArgs e)
      {
         this.DialogResult = false;
         this.Close();
      }

      void isAbilityLevelCheckBox_Checked(object sender, RoutedEventArgs e)
      {
         var isMedenia = isAbilityLevelCheckBox.IsChecked.Value;

         if (isMedenia)
         {
            levelText.Text = "Ability Level:";
            warriorCheckBox.Content = "_Gladiator";
            wizardCheckBox.Content = "_Summoner";
            priestCheckBox.Content = "_Bard";
            rogueCheckBox.Content = "_Archer";
            monkCheckBox.Content = "_Druid";
         }
         else
         {
            levelText.Text = "Level:";
            warriorCheckBox.Content = "W_arrior";
            wizardCheckBox.Content = "_Wizard";
            priestCheckBox.Content = "_Priest";
            rogueCheckBox.Content = "_Rogue";
            monkCheckBox.Content = "_Monk";
         }
      }
   }
}
