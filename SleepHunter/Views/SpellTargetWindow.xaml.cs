using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using SleepHunter.Extensions;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.Settings;

namespace SleepHunter.Views
{
    public partial class SpellTargetWindow : Window
    {
        private readonly SpellQueueItem spellQueueItem = new SpellQueueItem();

        public static readonly DependencyProperty IsEditModeProperty =
             DependencyProperty.Register("IsEditMode", typeof(bool), typeof(SpellTargetWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty SpellProperty =
            DependencyProperty.Register("Spell", typeof(Spell), typeof(SpellTargetWindow), new PropertyMetadata(null));

        public SpellQueueItem SpellQueueItem { get; private set; }

        public Spell Spell
        {
            get { return (Spell)GetValue(SpellProperty); }
            set { SetValue(SpellProperty, value); }
        }

        public bool IsEditMode
        {
            get { return (bool)GetValue(IsEditModeProperty); }
            set { SetValue(IsEditModeProperty, value); }
        }

        public SpellTargetWindow(Spell spell, SpellQueueItem item, bool isEditMode = true)
           : this(spell)
        {
            if (isEditMode)
            {
                Title = "Edit Target";
                okButton.Content = "_Save Changes";
            }

            SpellQueueItem.Id = item.Id;
            SetTargetForMode(item.Target);

            maxLevelCheckBox.IsChecked = item.HasTargetLevel;

            if (item.HasTargetLevel)
                maxLevelUpDown.Value = item.TargetLevel.Value;

            IsEditMode = isEditMode;
        }

        public SpellTargetWindow(Spell spell)
           : this()
        {
            Spell = spell;

            maxLevelUpDown.Value = spell.MaximumLevel;
            maxLevelCheckBox.IsChecked = spell.CurrentLevel < spell.MaximumLevel;

            if (spell.TargetMode == SpellTargetMode.None)
            {
                targetModeComboBox.SelectedValue = "None";
                targetModeComboBox.IsEnabled = false;
            }
            else
                targetModeComboBox.SelectedValue = "Self";

            if (!SpellMetadataManager.Instance.ContainsSpell(spell.Name))
            {
                // Warn on missing spell?
            }
        }

        public SpellTargetWindow()
        {
            InitializeComponent();
            InitializeViews();

            ToggleTargetMode(TargetCoordinateUnits.None);
        }

        private void InitializeViews()
        {
            PlayerManager.Instance.PlayerAdded += OnPlayerCollectionChanged;
            PlayerManager.Instance.PlayerUpdated += OnPlayerCollectionChanged;
            PlayerManager.Instance.PlayerRemoved += OnPlayerCollectionChanged;

            PlayerManager.Instance.PlayerPropertyChanged += OnPlayerPropertyChanged;
        }

        private void OnPlayerCollectionChanged(object sender, PlayerEventArgs e)
        {
            Dispatcher.InvokeIfRequired(() =>
            {
                BindingOperations.GetBindingExpression(characterComboBox, ListView.ItemsSourceProperty).UpdateTarget();

            }, DispatcherPriority.DataBind);
        }

        private void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is Player player))
                return;

            if (string.Equals("Name", e.PropertyName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals("IsLoggedIn", e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                Dispatcher.InvokeIfRequired(() =>
                {
                    BindingOperations.GetBindingExpression(characterComboBox, ListView.ItemsSourceProperty).UpdateTarget();
                    characterComboBox.Items.Refresh();

                }, DispatcherPriority.DataBind);
            }
        }

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

            if (Spell.TargetMode == SpellTargetMode.Target && selectedMode == TargetCoordinateUnits.None)
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

            if (selectedMode == TargetCoordinateUnits.Character && string.IsNullOrWhiteSpace(characterName))
            {
                this.ShowMessageBox("Invalid Character",
                   "Alternate character cannot be empty.",
                   "If the character you are looking for does not show up\nclose this window and try again.",
                   MessageBoxButton.OK,
                   440, 220);

                return false;
            }

            if ((selectedMode == TargetCoordinateUnits.RelativeRadius || selectedMode == TargetCoordinateUnits.AbsoluteRadius) &&
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

            if (!IsEditMode)
                spellQueueItem.StartingLevel = Spell.CurrentLevel;

            spellQueueItem.Target.Units = selectedMode;

            if (selectedMode == TargetCoordinateUnits.Character)
                spellQueueItem.Target.CharacterName = characterName;
            else
                spellQueueItem.Target.CharacterName = null;

            spellQueueItem.Target.Location = GetLocationForMode(selectedMode);
            spellQueueItem.Target.Offset = new Point(offsetXUpDown.Value, offsetYUpDown.Value);

            if (selectedMode == TargetCoordinateUnits.AbsoluteRadius || selectedMode == TargetCoordinateUnits.RelativeRadius)
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

        private TargetCoordinateUnits GetSelectedMode()
        {
            TargetCoordinateUnits mode = TargetCoordinateUnits.None;

            if (targetModeComboBox == null)
                return mode;

            if (!(targetModeComboBox.SelectedValue is string setting))
                return mode;

            Enum.TryParse(setting, out mode);
            return mode;
        }

        private Point GetLocationForMode(TargetCoordinateUnits units)
        {
            switch (units)
            {
                case TargetCoordinateUnits.AbsoluteTile:
                    return new Point(absoluteTileXUpDown.Value, absoluteTileYUpDown.Value);

                case TargetCoordinateUnits.AbsoluteXY:
                    return new Point(absoluteXUpDown.Value, absoluteYUpDown.Value);

                case TargetCoordinateUnits.RelativeTile:
                    return new Point((int)relativeTileXComboBox.SelectedValue, (int)relativeTileYComboBox.SelectedValue);

                case TargetCoordinateUnits.RelativeRadius:
                    goto case TargetCoordinateUnits.RelativeTile;

                case TargetCoordinateUnits.AbsoluteRadius:
                    goto case TargetCoordinateUnits.AbsoluteTile;

                default:
                    return new Point(0, 0);
            }
        }

        private void SetTargetForMode(SpellTarget target)
        {
            if (target == null)
                return;

            targetModeComboBox.SelectedValue = target.Units.ToString();

            switch (target.Units)
            {
                case TargetCoordinateUnits.Character:
                    characterComboBox.SelectedValue = target.CharacterName;
                    break;

                case TargetCoordinateUnits.AbsoluteTile:
                    absoluteTileXUpDown.Value = target.Location.X;
                    absoluteTileYUpDown.Value = target.Location.Y;
                    break;

                case TargetCoordinateUnits.AbsoluteXY:
                    absoluteXUpDown.Value = target.Location.X;
                    absoluteYUpDown.Value = target.Location.Y;
                    break;

                case TargetCoordinateUnits.RelativeTile:
                    relativeTileXComboBox.SelectedItem = (int)target.Location.X;
                    relativeTileYComboBox.SelectedItem = (int)target.Location.Y;
                    break;

                case TargetCoordinateUnits.RelativeRadius:
                    innerRadiusUpDown.Value = target.InnerRadius;
                    outerRadiusUpDown.Value = target.OuterRadius;
                    goto case TargetCoordinateUnits.RelativeTile;

                case TargetCoordinateUnits.AbsoluteRadius:
                    innerRadiusUpDown.Value = target.InnerRadius;
                    outerRadiusUpDown.Value = target.OuterRadius;
                    goto case TargetCoordinateUnits.AbsoluteTile;
            }

            offsetXUpDown.Value = target.Offset.X;
            offsetYUpDown.Value = target.Offset.Y;
        }

        private void ToggleTargetMode(TargetCoordinateUnits units)
        {
            var isRadius = units == TargetCoordinateUnits.AbsoluteRadius || units == TargetCoordinateUnits.RelativeRadius;

            if (characterComboBox != null)
                characterComboBox.Visibility = (units == TargetCoordinateUnits.Character) ? Visibility.Visible : Visibility.Collapsed;

            if (relativeTileXComboBox != null)
                relativeTileXComboBox.Visibility = (units == TargetCoordinateUnits.RelativeTile || units == TargetCoordinateUnits.RelativeRadius) ? Visibility.Visible : Visibility.Collapsed;

            if (absoluteTileXUpDown != null)
                absoluteTileXUpDown.Visibility = (units == TargetCoordinateUnits.AbsoluteTile || units == TargetCoordinateUnits.AbsoluteRadius) ? Visibility.Visible : Visibility.Collapsed;

            if (absoluteXUpDown != null)
                absoluteXUpDown.Visibility = (units == TargetCoordinateUnits.AbsoluteXY) ? Visibility.Visible : Visibility.Collapsed;

            if (offsetXUpDown != null)
                offsetXUpDown.Visibility = (units != TargetCoordinateUnits.None) ? Visibility.Visible : Visibility.Collapsed;

            if (innerRadiusUpDown != null)
                innerRadiusUpDown.Visibility = isRadius ? Visibility.Visible : Visibility.Collapsed;

            if (outerRadiusUpDown != null)
                outerRadiusUpDown.Visibility = isRadius ? Visibility.Visible : Visibility.Collapsed;

            // Do not show mouse offset for absolute x/y (screen position) as it is redundant
            if (offsetXUpDown != null)
                offsetXUpDown.Visibility = units != TargetCoordinateUnits.AbsoluteXY ? Visibility.Visible : Visibility.Collapsed;

            SizeToFit(units, IsLoaded);
        }

        private void SizeToFit(TargetCoordinateUnits units, bool animate = true)
        {
            var measuredHeight = 380;

            if (units == TargetCoordinateUnits.Character)
                measuredHeight += 42;
            else if (units == TargetCoordinateUnits.AbsoluteTile || units == TargetCoordinateUnits.RelativeTile)
                measuredHeight += 42;
            else if (units == TargetCoordinateUnits.AbsoluteRadius || units == TargetCoordinateUnits.RelativeRadius)
                measuredHeight += 84;

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
                ToggleTargetMode(TargetCoordinateUnits.None);
                return;
            }

            if (!(e.AddedItems[0] is UserSetting item))
            {
                ToggleTargetMode(TargetCoordinateUnits.None);
                return;
            }

            if (!Enum.TryParse<TargetCoordinateUnits>(item.Value as string, out var mode))
                mode = TargetCoordinateUnits.None;

            ToggleTargetMode(mode);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSpellTarget())
                return;

            DialogResult = true;
            Close();
        }
    }
}
