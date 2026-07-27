using System;
using System.Windows;

using SleepHunter.Extensions;
using SleepHunter.Metadata;
using SleepHunter.Models;

namespace SleepHunter.Views
{
    public partial class StaffEditorWindow : Window
    {
        private readonly string originalName;
        private readonly StaffMetadataManager staffMetadata;
        private StaffMetadata staff = new();

        public StaffMetadata Staff
        {
            get => staff;
            private set => staff = value;
        }

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(StaffEditorWindow), new PropertyMetadata(false));

        public StaffEditorWindow(
            StaffMetadata staff,
            StaffMetadataManager staffMetadata,
            bool isEditMode = true)
           : this(staffMetadata)
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

            SetCharacterClassFlags(staff.Class);

            IsEditMode = isEditMode;

            if (isEditMode)
                Title = "Edit Staff";
        }

        public StaffEditorWindow(
            StaffMetadataManager staffMetadata)
        {
            this.staffMetadata = staffMetadata ??
                throw new ArgumentNullException(
                    nameof(staffMetadata));
            InitializeComponent();

            Title = "Add Staff";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nameTextBox.Focus();
            nameTextBox.SelectAll();
        }

        private bool ValidateStaff()
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

            if (nameChanged &&
                staffMetadata.ContainsStaff(staffName))
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
            staff.Class = GetCharacterClassFlags();
            return true;
        }

        private CharacterClassFlags GetCharacterClassFlags()
        {
            var playerClass = CharacterClassFlags.Peasant;

            if (warriorCheckBox.IsChecked.Value)
                playerClass |= CharacterClassFlags.Warrior;

            if (wizardCheckBox.IsChecked.Value)
                playerClass |= CharacterClassFlags.Wizard;

            if (priestCheckBox.IsChecked.Value)
                playerClass |= CharacterClassFlags.Priest;

            if (rogueCheckBox.IsChecked.Value)
                playerClass |= CharacterClassFlags.Rogue;

            if (monkCheckBox.IsChecked.Value)
                playerClass |= CharacterClassFlags.Monk;

            return playerClass;
        }

        private void SetCharacterClassFlags(CharacterClassFlags playerClass)
        {
            warriorCheckBox.IsChecked = playerClass.HasFlag(CharacterClassFlags.Warrior);
            wizardCheckBox.IsChecked = playerClass.HasFlag(CharacterClassFlags.Wizard);
            priestCheckBox.IsChecked = playerClass.HasFlag(CharacterClassFlags.Priest);
            rogueCheckBox.IsChecked = playerClass.HasFlag(CharacterClassFlags.Rogue);
            monkCheckBox.IsChecked = playerClass.HasFlag(CharacterClassFlags.Monk);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateStaff())
                return;

            DialogResult = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void isAbilityLevelCheckBox_Checked(object sender, RoutedEventArgs e)
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
