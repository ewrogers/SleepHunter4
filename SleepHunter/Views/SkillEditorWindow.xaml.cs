using System;
using System.Windows;

using SleepHunter.Extensions;
using SleepHunter.Metadata;
using SleepHunter.Models;

namespace SleepHunter.Views
{
    internal partial class SkillEditorWindow : Window
    {
        private string originalName;
        private SkillMetadata skill = new SkillMetadata();

        public SkillMetadata Skill
        {
            get { return skill; }
            private set { skill = value; }
        }

        public bool IsEditMode
        {
            get { return (bool)GetValue(IsEditModeProperty); }
            set { SetValue(IsEditModeProperty, value); }
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register("IsEditMode", typeof(bool), typeof(SkillEditorWindow), new PropertyMetadata(false));

        public SkillEditorWindow(SkillMetadata skill, bool isEditMode = true)
           : this()
        {
            nameTextBox.Text = originalName = skill.Name;
            groupNameTextBox.Text = skill.GroupName;
            manaUpDown.Value = skill.ManaCost;
            cooldownTextBox.Text = skill.Cooldown.ToShortEnglish();
            assailCheckBox.IsChecked = skill.IsAssail;
            dialogCheckBox.IsChecked = skill.OpensDialog;
            improveCheckBox.IsChecked = !skill.CanImprove;
            disarmCheckBox.IsChecked = skill.RequiresDisarm;

            SetPlayerClass(skill.Class);

            IsEditMode = isEditMode;

            if (isEditMode)
                Title = "Edit Skill";
        }

        public SkillEditorWindow()
        {
            InitializeComponent();
            Title = "Add Skill";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nameTextBox.Focus();
            nameTextBox.SelectAll();
        }

        private bool ValidateSkill()
        {
            string skillName = nameTextBox.Text.Trim();
            string groupName = groupNameTextBox.Text.Trim();
            int manaCost = (int)manaUpDown.Value;
            TimeSpan cooldown;
            bool isAssail = assailCheckBox.IsChecked.Value;
            bool opensDialog = dialogCheckBox.IsChecked.Value;
            bool doesNotLevel = improveCheckBox.IsChecked.Value;
            bool requiresDisarm = disarmCheckBox.IsChecked.Value;
            bool nameChanged = originalName == null || !string.Equals(originalName, skillName, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(skillName))
            {
                this.ShowMessageBox("Invalid Name",
                   "Skill names must not be null or empty.",
                   "This includes whitespace characters.",
                   MessageBoxButton.OK,
                   420, 220);

                nameTextBox.Focus();
                nameTextBox.SelectAll();
                return false;
            }

            if (nameChanged && SkillMetadataManager.Instance.ContainsSkill(skillName))
            {
                this.ShowMessageBox("Duplicate Name",
                   "A skill already exists with the same name.",
                   "Skill names are case-insenstive.",
                   MessageBoxButton.OK,
                   420, 220);

                nameTextBox.Focus();
                nameTextBox.SelectAll();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cooldownTextBox.Text.Trim()))
                cooldown = TimeSpan.Zero;
            else if (double.TryParse(cooldownTextBox.Text.Trim(), out var cooldownSeconds) && cooldownSeconds >= 0)
                cooldown = TimeSpan.FromSeconds(cooldownSeconds);
            else if (!TimeSpanExtender.TryParse(cooldownTextBox.Text.Trim(), out cooldown) || cooldown < TimeSpan.Zero)
            {
                this.ShowMessageBox("Invalid Cooldown",
                   "Cooldown must be a valid positive timespan value.",
                   "You may use fractional units of days, hours, minutes, and seconds.\nYou may also leave it blank for zero cooldown.",
                   MessageBoxButton.OK,
                   420, 240);

                cooldownTextBox.Focus();
                cooldownTextBox.SelectAll();
                return false;
            }

            skill.Name = skillName;
            skill.GroupName = string.IsNullOrWhiteSpace(groupName) ? null : groupName;
            skill.Class = GetPlayerClass();
            skill.ManaCost = manaCost;
            skill.Cooldown = cooldown;
            skill.IsAssail = isAssail;
            skill.OpensDialog = opensDialog;
            skill.CanImprove = !doesNotLevel;
            skill.RequiresDisarm = requiresDisarm;
            return true;
        }

        private PlayerClass GetPlayerClass()
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

        private void SetPlayerClass(PlayerClass playerClass)
        {
            warriorCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Warrior);
            wizardCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Wizard);
            priestCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Priest);
            rogueCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Rogue);
            monkCheckBox.IsChecked = playerClass.HasFlag(PlayerClass.Monk);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSkill())
                return;

            DialogResult = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
