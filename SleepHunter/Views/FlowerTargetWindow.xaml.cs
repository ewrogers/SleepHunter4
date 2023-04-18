using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using SleepHunter.Extensions;
using SleepHunter.Models;
using SleepHunter.Settings;

namespace SleepHunter.Views
{
    internal partial class FlowerTargetWindow : Window
    {
        private FlowerQueueItem flowerQueueItem = new FlowerQueueItem();

        public FlowerQueueItem FlowerQueueItem
        {
            get { return flowerQueueItem; }
            private set { flowerQueueItem = value; }
        }

        public bool IsEditMode
        {
            get { return (bool)GetValue(IsEditModeProperty); }
            set { SetValue(IsEditModeProperty, value); }
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register("IsEditMode", typeof(bool), typeof(FlowerTargetWindow), new PropertyMetadata(false));

        public FlowerTargetWindow(FlowerQueueItem item, bool isEditMode = true)
           : this()
        {
            if (isEditMode)
            {
                Title = "Edit Target";
                okButton.Content = "_Save Changes";
            }

            FlowerQueueItem.Id = item.Id;
            SetTargetForMode(item.Target);

            if (item.Interval.HasValue)
                intervalTextBox.Text = item.Interval.Value.ToShortEnglish();
            else
                intervalTextBox.Text = string.Empty;

            intervalCheckBox.IsChecked = item.Interval.HasValue;


            if (item.ManaThreshold.HasValue)
                manaThresholdUpDown.Value = item.ManaThreshold.Value;
            else
                manaThresholdUpDown.Value = 1000;

            manaThresholdCheckBox.IsChecked = item.ManaThreshold.HasValue;

            IsEditMode = isEditMode;
        }

        public FlowerTargetWindow()
        {
            InitializeComponent();
            InitializeViews();

            ToggleTargetMode(TargetCoordinateUnits.Character);
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

        private bool ValidateFlowerTarget()
        {
            var selectedMode = GetSelectedMode();
            var interval = TimeSpan.Zero;

            #region Check Target Mode
            if (selectedMode == TargetCoordinateUnits.None)
            {
                this.ShowMessageBox("Target Required",
                   "Lyliac Plant requires a target.",
                   "You must select a target mode from the dropdown list.",
                   MessageBoxButton.OK);

                targetModeComboBox.Focus();
                targetModeComboBox.IsDropDownOpen = true;
                return false;
            }
            #endregion

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

            if (intervalCheckBox.IsChecked.Value)
            {
                if (string.IsNullOrWhiteSpace(intervalTextBox.Text.Trim()))
                    interval = TimeSpan.Zero;
                else if (double.TryParse(intervalTextBox.Text.Trim(), out var intervalSeconds) && intervalSeconds >= 0)
                    interval = TimeSpan.FromSeconds(intervalSeconds);
                else if (!TimeSpanExtender.TryParse(intervalTextBox.Text.Trim(), out interval) || interval < TimeSpan.Zero)
                {
                    this.ShowMessageBox("Invalid Interval",
                       "Interval must be a valid positive timespan value.",
                       "You may use fractional units of days, hours, minutes, and seconds.\nYou may also leave it blank for continuous.",
                       MessageBoxButton.OK,
                       420, 240);

                    intervalTextBox.Focus();
                    intervalTextBox.SelectAll();
                    return false;
                }
            }

            if (!intervalCheckBox.IsChecked.Value && !manaThresholdCheckBox.IsChecked.Value)
            {
                this.ShowMessageBox("Missing Condition",
                   "You must specify at least one condition to flower on.\nYou may use the time interval or mana conditions, or both.",
                   "If you specify both, it will flower if either condition is reached.",
                   MessageBoxButton.OK,
                   480, 240);

                return false;
            }

            flowerQueueItem.Target.Units = selectedMode;

            if (selectedMode == TargetCoordinateUnits.Character)
                flowerQueueItem.Target.CharacterName = characterName;
            else
                flowerQueueItem.Target.CharacterName = null;

            flowerQueueItem.Target.Location = GetLocationForMode(selectedMode);
            flowerQueueItem.Target.Offset = new Point(offsetXUpDown.Value, offsetYUpDown.Value);

            if (selectedMode == TargetCoordinateUnits.AbsoluteRadius || selectedMode == TargetCoordinateUnits.RelativeRadius)
            {
                flowerQueueItem.Target.InnerRadius = (int)innerRadiusUpDown.Value;
                flowerQueueItem.Target.OuterRadius = (int)outerRadiusUpDown.Value;
            }
            else
            {
                flowerQueueItem.Target.InnerRadius = 0;
                flowerQueueItem.Target.OuterRadius = 0;
            }

            if (intervalCheckBox.IsChecked.Value)
                flowerQueueItem.Interval = interval;
            else
                flowerQueueItem.Interval = null;

            if (manaThresholdUpDown.IsEnabled && manaThresholdCheckBox.IsChecked.Value)
                flowerQueueItem.ManaThreshold = (int)manaThresholdUpDown.Value;
            else
                flowerQueueItem.ManaThreshold = null;

            return true;
        }

        private TargetCoordinateUnits GetSelectedMode()
        {
            var mode = TargetCoordinateUnits.None;

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

            if (manaThresholdCheckBox != null)
            {
                manaThresholdCheckBox.IsEnabled = (units == TargetCoordinateUnits.Character);

                if (!manaThresholdCheckBox.IsEnabled)
                    manaThresholdCheckBox.IsChecked = false;
            }

            SizeToFit(units, IsLoaded);
        }

        private void SizeToFit(TargetCoordinateUnits units, bool animate = true)
        {
            var measuredHeight = 346;

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
            if (!ValidateFlowerTarget())
                return;

            DialogResult = true;
            Close();
        }
    }
}
