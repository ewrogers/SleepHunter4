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
        private readonly SpellMetadataManager spellMetadata;
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


        public SpellEditorWindow(
            SpellMetadata spell,
            SpellMetadataManager spellMetadata,
            bool isEditMode = true)
           : this(spellMetadata)
        {
            nameTextBox.Text = originalName = spell.Name;
            groupNameTextBox.Text = spell.GroupName;
            manaUpDown.Value = spell.ManaCost;
            linesUpDown.Value = spell.NumberOfLines;
            cooldownTextBox.Text = spell.Cooldown.ToShortEnglish();

            // conditions
            minHpPercentCheckBox.IsChecked = spell.MinHealthPercent > 0;
            minHpPercentUpDown.Value = spell.MinHealthPercent;

            maxHpPercentCheckBox.IsChecked = spell.MaxHealthPercent > 0;
            maxHpPercentUpDown.Value = spell.MaxHealthPercent;

            // options
            dialogCheckBox.IsChecked = spell.OpensDialog;
            improveCheckBox.IsChecked = !spell.CanImprove;

            SetCharacterClassFlags(spell.Class);

            IsEditMode = isEditMode;

            if (isEditMode)
            {
                Title = "Edit Spell";
                okButton.Content = "_Save Changes";
            }
        }

        public SpellEditorWindow(
            SpellMetadataManager spellMetadata)
        {
            this.spellMetadata = spellMetadata ??
                throw new ArgumentNullException(
                    nameof(spellMetadata));
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
            var spellName = nameTextBox.Text.Trim();
            var groupName = groupNameTextBox.Text.Trim();
            var manaCost = (int)manaUpDown.Value;
            var numberOfLines = (int)linesUpDown.Value;
            TimeSpan cooldown;

            // conditions
            var minHpPercent = (minHpPercentCheckBox.IsChecked ?? false) ? minHpPercentUpDown.Value : 0;
            var maxHpPercent = (maxHpPercentCheckBox.IsChecked ?? false) ? maxHpPercentUpDown.Value : 0;

            // options
            var opensDialog = dialogCheckBox.IsChecked.Value;
            var doesNotLevel = improveCheckBox.IsChecked.Value;
            var nameChanged = originalName == null || !string.Equals(originalName, spellName, StringComparison.OrdinalIgnoreCase);

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

            if (nameChanged &&
                spellMetadata.ContainsSpell(spellName))
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

            spell.Name = spellName;
            spell.GroupName = string.IsNullOrWhiteSpace(groupName) ? null : groupName;
            spell.Class = GetCharacterClassFlags();
            spell.ManaCost = manaCost;
            spell.NumberOfLines = numberOfLines;
            spell.Cooldown = cooldown;

            spell.MinHealthPercent = minHpPercent;
            spell.MaxHealthPercent = maxHpPercent;

            spell.OpensDialog = opensDialog;
            spell.CanImprove = !doesNotLevel;
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
