using System;
using System.Windows;

using SleepHunter.Extensions;
using SleepHunter.Metadata;
using SleepHunter.Models;

namespace SleepHunter.Views
{
    public partial class SpellEditorWindow : Window
    {
        private readonly string originalName;
        private SpellMetadata spell = new();

        public SpellMetadata Spell
        {
            get => spell;
            private set => spell = value;
        }

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(SpellEditorWindow), new PropertyMetadata(false));


        public SpellEditorWindow(SpellMetadata spell, bool isEditMode = true)
           : this()
        {
            nameTextBox.Text = originalName = spell.Name;
            groupNameTextBox.Text = spell.GroupName;
            manaUpDown.Value = spell.ManaCost;
            linesUpDown.Value = spell.NumberOfLines;
            cooldownTextBox.Text = spell.Cooldown.ToShortEnglish();
            improveCheckBox.IsChecked = !spell.CanImprove;

            SetPlayerClass(spell.Class);

            IsEditMode = isEditMode;

            if (isEditMode)
            {
                Title = "Edit Spell";
                okButton.Content = "_Save Changes";
            }
        }

        public SpellEditorWindow()
        {
            InitializeComponent();
            Title = "Add Spell";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nameTextBox.Focus();
            nameTextBox.SelectAll();
        }

        private bool ValidateSpell()
        {
            string spellName = nameTextBox.Text.Trim();
            string groupName = groupNameTextBox.Text.Trim();
            int manaCost = (int)manaUpDown.Value;
            int numberOfLines = (int)linesUpDown.Value;
            TimeSpan cooldown;
            bool doesNotLevel = improveCheckBox.IsChecked.Value;
            bool nameChanged = originalName == null || !string.Equals(originalName, spellName, StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(spellName))
            {
                this.ShowMessageBox("Invalid Name",
                   "Spell names must not be null or empty.",
                   "This includes whitespace characters.",
                   MessageBoxButton.OK,
                   420, 220);

                nameTextBox.Focus();
                nameTextBox.SelectAll();
                return false;
            }

            if (nameChanged && SpellMetadataManager.Instance.ContainsSpell(spellName))
            {
                this.ShowMessageBox("Duplicate Name",
                   "A spell already exists with the same name.",
                   "Spell names are case-insenstive.",
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

            spell.Name = spellName;
            spell.GroupName = string.IsNullOrWhiteSpace(groupName) ? null : groupName;
            spell.Class = GetPlayerClass();
            spell.ManaCost = manaCost;
            spell.NumberOfLines = numberOfLines;
            spell.Cooldown = cooldown;
            spell.CanImprove = !doesNotLevel;
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
            if (!ValidateSpell())
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
