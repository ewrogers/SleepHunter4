using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using SleepHunter.Extensions;
using SleepHunter.Models;
using SleepHunter.Settings;

namespace SleepHunter.Views
{
    public partial class FlowerTargetWindow : Window
    {
        private FlowerQueueItem flowerQueueItem = new FlowerQueueItem();

        public FlowerQueueItem FlowerQueueItem
        {
            get => flowerQueueItem;
            private set => flowerQueueItem = value;
        }

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(FlowerTargetWindow), new PropertyMetadata(false));

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

            ToggleTargetMode(SpellTargetMode.Character);
        }

        private void InitializeViews()
        {
            PlayerManager.Instance.PlayerAdded += OnPlayerCollectionChanged;
            PlayerManager.Instance.PlayerRemoved += OnPlayerCollectionChanged;

            PlayerManager.Instance.PlayerPropertyChanged += OnPlayerPropertyChanged;
        }

        private async void OnPlayerCollectionChanged(object sender, PlayerEventArgs e)
        {
            await Dispatcher.SwitchToUIThread();

            BindingOperations.GetBindingExpression(characterComboBox, ItemsControl.ItemsSourceProperty).UpdateTarget();
        }

        private async void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not Player player)
                return;

            await Dispatcher.SwitchToUIThread();

            if (string.Equals(nameof(player.Name), e.PropertyName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(nameof(player.IsLoggedIn), e.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                BindingOperations.GetBindingExpression(characterComboBox, ItemsControl.ItemsSourceProperty).UpdateTarget();
                characterComboBox.Items.Refresh();
            }
        }

        private bool ValidateFlowerTarget()
        {
            var selectedMode = GetSelectedMode();
            var interval = TimeSpan.Zero;

            if (selectedMode == SpellTargetMode.None)
            {
                this.ShowMessageBox("Target Required",
                   "Lyliac Plant requires a target.",
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

            if (intervalCheckBox.IsChecked.Value)
            {
                if (string.IsNullOrWhiteSpace(intervalTextBox.Text.Trim()))
                    interval = TimeSpan.Zero;
                else if (double.TryParse(intervalTextBox.Text.Trim(), out var intervalSeconds) && intervalSeconds >= 0)
                    interval = TimeSpan.FromSeconds(intervalSeconds);
                else if (!TimeSpanExtensions.TryParse(intervalTextBox.Text.Trim(), out interval) || interval < TimeSpan.Zero)
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

            flowerQueueItem.Target.Mode = selectedMode;

            if (selectedMode == SpellTargetMode.Character)
                flowerQueueItem.Target.CharacterName = characterName;
            else
                flowerQueueItem.Target.CharacterName = null;

            flowerQueueItem.Target.Location = GetLocationForMode(selectedMode);
            flowerQueueItem.Target.Offset = new Point(offsetXUpDown.Value, offsetYUpDown.Value);

            if (selectedMode == SpellTargetMode.AbsoluteRadius || selectedMode == SpellTargetMode.RelativeRadius)
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

        private SpellTargetMode GetSelectedMode()
        {
            var mode = SpellTargetMode.None;

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

        private void SetTargetForMode(SpellTarget target)
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

            if (manaThresholdCheckBox != null)
            {
                manaThresholdCheckBox.IsEnabled = (units == SpellTargetMode.Character);

                if (!manaThresholdCheckBox.IsEnabled)
                    manaThresholdCheckBox.IsChecked = false;
            }

            SizeToFit(units, IsLoaded);
        }

        private void SizeToFit(SpellTargetMode units, bool animate = true)
        {
            var measuredHeight = 346;

            if (units == SpellTargetMode.Character)
                measuredHeight += 42;
            else if (units == SpellTargetMode.AbsoluteTile || units == SpellTargetMode.RelativeTile)
                measuredHeight += 42;
            else if (units == SpellTargetMode.AbsoluteRadius || units == SpellTargetMode.RelativeRadius)
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
                ToggleTargetMode(SpellTargetMode.None);
                return;
            }

            if (!(e.AddedItems[0] is UserSetting item))
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
            if (!ValidateFlowerTarget())
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
