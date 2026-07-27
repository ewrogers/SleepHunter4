using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

using SleepHunter.Extensions;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Settings;
using SleepHunter.ViewModels.Editing;
using SleepHunter.ViewModels.Presentation;

namespace SleepHunter.Views
{
    public partial class SpellTargetWindow : Window
    {
        private SpellQueueItemViewModel spellQueueItem = new();

        public SpellQueueItemViewModel SpellQueueItemViewModel
        {
            get => spellQueueItem;
            private set => spellQueueItem = value;
        }

        public SpellViewModel Spell
        {
            get => (SpellViewModel)GetValue(SpellProperty);
            set => SetValue(SpellProperty, value);
        }

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(SpellTargetWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty SpellProperty =
            DependencyProperty.Register(
                nameof(Spell),
                typeof(SpellViewModel),
                typeof(SpellTargetWindow),
                new PropertyMetadata(null));

        public SpellTargetWindow(
            SpellViewModel spell,
            SpellQueueItemViewModel item,
            IEnumerable<string> characterNames,
            bool isEditMode = true)
           : this(spell, characterNames)
        {
            if (isEditMode)
            {
                Title = "Edit Target";
                okButton.Content = "_Save Changes";
            }

            SpellQueueItemViewModel.Id = item.Id;
            SetTargetForMode(item.Target);

            maxLevelCheckBox.IsChecked = item.HasTargetLevel;

            if (item.HasTargetLevel)
                maxLevelUpDown.Value = item.TargetLevel.Value;

            IsEditMode = isEditMode;
        }

        public SpellTargetWindow(
            SpellViewModel spell,
            IEnumerable<string> characterNames)
           : this(characterNames)
        {
            Spell = spell;

            maxLevelUpDown.Value = spell.MaximumLevel;
            maxLevelCheckBox.IsChecked = spell.CurrentLevel < spell.MaximumLevel;

            if (spell.ArgumentType == SpellArgumentType.None)
            {
                targetModeComboBox.SelectedValue = "None";
                targetModeComboBox.IsEnabled = false;
            }
            else
            {
                targetModeComboBox.Items.RemoveAt(0);
                targetModeComboBox.SelectedValue = "Self";
            }

        }

        public SpellTargetWindow(
            IEnumerable<string> characterNames = null)
        {
            InitializeComponent();
            characterComboBox.ItemsSource =
                NormalizeCharacterNames(characterNames);

            ToggleTargetMode(SpellTargetMode.None);
        }

        private static string[] NormalizeCharacterNames(
            IEnumerable<string> characterNames) =>
            characterNames?
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToArray() ??
            [];

        private bool ValidateSpellTarget()
        {
            if (Spell == null)
            {
                this.ShowMessageBox("Invalid Spell",
                   "This spell is no longer valid.",
                   "This spell window will now close, please try again.",
                   MessageBoxButton.OK);

                Close();
                return false;
            }

            var selectedMode = GetSelectedMode();

            if (Spell.ArgumentType == SpellArgumentType.Target &&
                selectedMode == SpellTargetMode.None)
            {
                this.ShowMessageBox("Target Required",
                   "This spell requires a target.",
                   "You must select a target mode from the dropdown list.",
                   MessageBoxButton.OK);

                targetModeComboBox.Focus();
                targetModeComboBox.IsDropDownOpen = true;
                return false;
            }

            var characterName = characterComboBox.SelectedValue as string;

            if (selectedMode == SpellTargetMode.Character && string.IsNullOrWhiteSpace(characterName))
            {
                this.ShowMessageBox("Invalid Character",
                   "Alternate character cannot be empty.",
                   "If the character you are looking for does not show up\nclose this window and try again.",
                   MessageBoxButton.OK,
                   440, 220);

                return false;
            }

            if ((selectedMode == SpellTargetMode.RelativeRadius || selectedMode == SpellTargetMode.AbsoluteRadius) &&
               innerRadiusUpDown.Value > outerRadiusUpDown.Value)
            {
                this.ShowMessageBox("Invalid Radius",
                   "The inner radius must be less than or equal to the outer radius.",
                   "You may use zero inner radius to include yourself, one to start from adjacent tiles",
                   MessageBoxButton.OK,
                   440, 220);

                return false;
            }

            spellQueueItem.Icon = Spell.Icon;
            spellQueueItem.Name = Spell.Name;
            spellQueueItem.CurrentLevel = Spell.CurrentLevel;
            spellQueueItem.MaximumLevel = Spell.MaximumLevel;
            spellQueueItem.HealthCondition = new HealthCondition(
                Spell.MinHealthPercent,
                Spell.MaxHealthPercent);

            if (!IsEditMode)
                spellQueueItem.StartingLevel = Spell.CurrentLevel;

            spellQueueItem.Target.Mode = selectedMode;

            if (selectedMode == SpellTargetMode.Character)
                spellQueueItem.Target.CharacterName = characterName;
            else
                spellQueueItem.Target.CharacterName = null;

            spellQueueItem.Target.Location = GetLocationForMode(selectedMode);
            spellQueueItem.Target.Offset = new Point(offsetXUpDown.Value, offsetYUpDown.Value);

            if (selectedMode == SpellTargetMode.AbsoluteRadius || selectedMode == SpellTargetMode.RelativeRadius)
            {
                spellQueueItem.Target.InnerRadius = (int)innerRadiusUpDown.Value;
                spellQueueItem.Target.OuterRadius = (int)outerRadiusUpDown.Value;
            }
            else
            {
                spellQueueItem.Target.InnerRadius = 0;
                spellQueueItem.Target.OuterRadius = 0;
            }

            if (!maxLevelCheckBox.IsChecked.Value)
                spellQueueItem.TargetLevel = null;
            else
                spellQueueItem.TargetLevel = (int)maxLevelUpDown.Value;

            return true;
        }

        private SpellTargetMode GetSelectedMode()
        {
            SpellTargetMode mode = SpellTargetMode.None;

            if (targetModeComboBox == null)
                return mode;

            if (!(targetModeComboBox.SelectedValue is string setting))
                return mode;

            Enum.TryParse(setting, out mode);
            return mode;
        }

        private Point GetLocationForMode(SpellTargetMode units)
        {
            switch (units)
            {
                case SpellTargetMode.AbsoluteTile:
                    return new Point(absoluteTileXUpDown.Value, absoluteTileYUpDown.Value);

                case SpellTargetMode.AbsoluteXY:
                    return new Point(absoluteXUpDown.Value, absoluteYUpDown.Value);

                case SpellTargetMode.RelativeTile:
                    return new Point((int)relativeTileXComboBox.SelectedValue, (int)relativeTileYComboBox.SelectedValue);

                case SpellTargetMode.RelativeRadius:
                    goto case SpellTargetMode.RelativeTile;

                case SpellTargetMode.AbsoluteRadius:
                    goto case SpellTargetMode.AbsoluteTile;

                default:
                    return new Point(0, 0);
            }
        }

        private void SetTargetForMode(
            SpellTargetViewModel target)
        {
            if (target == null)
                return;

            targetModeComboBox.SelectedValue = target.Mode.ToString();

            switch (target.Mode)
            {
                case SpellTargetMode.Character:
                    characterComboBox.SelectedValue = target.CharacterName;
                    break;

                case SpellTargetMode.AbsoluteTile:
                    absoluteTileXUpDown.Value = target.Location.X;
                    absoluteTileYUpDown.Value = target.Location.Y;
                    break;

                case SpellTargetMode.AbsoluteXY:
                    absoluteXUpDown.Value = target.Location.X;
                    absoluteYUpDown.Value = target.Location.Y;
                    break;

                case SpellTargetMode.RelativeTile:
                    relativeTileXComboBox.SelectedItem = (int)target.Location.X;
                    relativeTileYComboBox.SelectedItem = (int)target.Location.Y;
                    break;

                case SpellTargetMode.RelativeRadius:
                    innerRadiusUpDown.Value = target.InnerRadius;
                    outerRadiusUpDown.Value = target.OuterRadius;
                    goto case SpellTargetMode.RelativeTile;

                case SpellTargetMode.AbsoluteRadius:
                    innerRadiusUpDown.Value = target.InnerRadius;
                    outerRadiusUpDown.Value = target.OuterRadius;
                    goto case SpellTargetMode.AbsoluteTile;
            }

            offsetXUpDown.Value = target.Offset.X;
            offsetYUpDown.Value = target.Offset.Y;
        }

        private void ToggleTargetMode(SpellTargetMode units)
        {
            var isRadius = units == SpellTargetMode.AbsoluteRadius || units == SpellTargetMode.RelativeRadius;

            if (characterComboBox != null)
                characterComboBox.Visibility = (units == SpellTargetMode.Character) ? Visibility.Visible : Visibility.Collapsed;

            if (relativeTileXComboBox != null)
                relativeTileXComboBox.Visibility = (units == SpellTargetMode.RelativeTile || units == SpellTargetMode.RelativeRadius) ? Visibility.Visible : Visibility.Collapsed;

            if (absoluteTileXUpDown != null)
                absoluteTileXUpDown.Visibility = (units == SpellTargetMode.AbsoluteTile || units == SpellTargetMode.AbsoluteRadius) ? Visibility.Visible : Visibility.Collapsed;

            if (absoluteXUpDown != null)
                absoluteXUpDown.Visibility = (units == SpellTargetMode.AbsoluteXY) ? Visibility.Visible : Visibility.Collapsed;

            if (offsetXUpDown != null)
                offsetXUpDown.Visibility = (units != SpellTargetMode.None && units != SpellTargetMode.AbsoluteXY) ? Visibility.Visible : Visibility.Collapsed;

            if (innerRadiusUpDown != null)
                innerRadiusUpDown.Visibility = isRadius ? Visibility.Visible : Visibility.Collapsed;

            if (outerRadiusUpDown != null)
                outerRadiusUpDown.Visibility = isRadius ? Visibility.Visible : Visibility.Collapsed;

            SizeToFit(units, IsLoaded);
        }

        private void SizeToFit(SpellTargetMode units, bool animate = true)
        {
            var measuredHeight = 380;

            if (units == SpellTargetMode.Character)
                measuredHeight += 42;
            else if (units == SpellTargetMode.AbsoluteTile || units == SpellTargetMode.RelativeTile)
                measuredHeight += 42;
            else if (units == SpellTargetMode.AbsoluteRadius || units == SpellTargetMode.RelativeRadius)
                measuredHeight += 84;
            else if (units == SpellTargetMode.None)
                measuredHeight -= 42;

            if (!animate)
            {
                Height = measuredHeight;
                return;
            }

            var heightAnimation = new DoubleAnimation(measuredHeight, new Duration(TimeSpan.FromSeconds(0.25)));
            BeginAnimation(HeightProperty, heightAnimation);
        }

        private void targetModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1)
            {
                ToggleTargetMode(SpellTargetMode.None);
                return;
            }

            if (e.AddedItems[0] is not UserSetting item)
            {
                ToggleTargetMode(SpellTargetMode.None);
                return;
            }

            if (!Enum.TryParse<SpellTargetMode>(item.Value as string, out var mode))
                mode = SpellTargetMode.None;

            ToggleTargetMode(mode);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSpellTarget())
                return;

            DialogResult = true;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }
    }
}
