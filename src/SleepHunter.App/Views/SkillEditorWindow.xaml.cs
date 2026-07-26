using System;
using System.Windows;

using SleepHunter.Extensions;
using SleepHunter.Metadata;
using SleepHunter.Models;

namespace SleepHunter.Views
{
    public partial class SkillEditorWindow : Window
    {
        private readonly string originalName;
        private SkillMetadata skill = new();

        public SkillMetadata Skill
        {
            get => skill;
            private set => skill = value;
        }

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(SkillEditorWindow), new PropertyMetadata(false));

        public SkillEditorWindow(SkillMetadata skill, bool isEditMode = true)
           : this()
        {
            nameTextBox.Text = originalName = skill.Name;
            groupNameTextBox.Text = skill.GroupName;
            manaUpDown.Value = skill.ManaCost;
            cooldownTextBox.Text = skill.Cooldown.ToShortEnglish();

            // conditions
            minHpPercentCheckBox.IsChecked = skill.MinHealthPercent > 0;
            minHpPercentUpDown.Value = skill.MinHealthPercent;

            maxHpPercentCheckBox.IsChecked = skill.MaxHealthPercent > 0;
            maxHpPercentUpDown.Value = skill.MaxHealthPercent;

            // options
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
            var skillName = nameTextBox.Text.Trim();
            var groupName = groupNameTextBox.Text.Trim();
            var manaCost = (int)manaUpDown.Value;
            TimeSpan cooldown;

            // conditions
            var minHpPercent = (minHpPercentCheckBox.IsChecked ?? false) ? minHpPercentUpDown.Value : 0;
            var maxHpPercent = (maxHpPercentCheckBox.IsChecked ?? false) ? maxHpPercentUpDown.Value : 0;

            // options
            var isAssail = assailCheckBox.IsChecked.Value;
            var opensDialog = dialogCheckBox.IsChecked.Value;
            var doesNotLevel = improveCheckBox.IsChecked.Value;
            var requiresDisarm = disarmCheckBox.IsChecked.Value;
            var nameChanged = originalName == null || !string.Equals(originalName, skillName, StringComparison.OrdinalIgnoreCase);

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
            else if (!TimeSpanExtensions.TryParse(cooldownTextBox.Text.Trim(), out cooldown) || cooldown < TimeSpan.Zero)
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

            skill.MinHealthPercent = minHpPercent;
            skill.MaxHealthPercent = maxHpPercent;

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
