using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using SleepHunter.Data;
using SleepHunter.Settings;

namespace SleepHunter
{
   public partial class FlowerTargetWindow : Window
   {
      FlowerQueueItem flowerQueueItem = new FlowerQueueItem();

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
            this.Title = "Edit Target";
            okButton.Content = "_Save Changes";
         }

         this.FlowerQueueItem.Id = item.Id;
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

         this.IsEditMode = isEditMode;
      }

      public FlowerTargetWindow()
      {
         InitializeComponent();
         InitializeViews();

         ToggleTargetMode(TargetCoordinateUnits.Character);
      }

      void InitializeViews()
      {
         PlayerManager.Instance.PlayerAdded += OnPlayerCollectionChanged;
         PlayerManager.Instance.PlayerUpdated += OnPlayerCollectionChanged;
         PlayerManager.Instance.PlayerRemoved += OnPlayerCollectionChanged;

         PlayerManager.Instance.PlayerPropertyChanged += OnPlayerPropertyChanged;
      }

      void OnPlayerCollectionChanged(object sender, PlayerEventArgs e)
      {
         this.Dispatcher.InvokeIfRequired(() =>
         {
            BindingOperations.GetBindingExpression(characterComboBox, ListView.ItemsSourceProperty).UpdateTarget();

         }, DispatcherPriority.DataBind);
      }

      void OnPlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         var player = sender as Player;
         if (player == null) return;

         if (string.Equals("Name", e.PropertyName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals("IsLoggedIn", e.PropertyName, StringComparison.OrdinalIgnoreCase))
         {
            this.Dispatcher.InvokeIfRequired(() =>
            {
               BindingOperations.GetBindingExpression(characterComboBox, ListView.ItemsSourceProperty).UpdateTarget();
               characterComboBox.Items.Refresh();

            }, DispatcherPriority.DataBind);
         }
      }

      bool ValidateFlowerTarget()
      {
         var selectedMode = GetSelectedMode();
         TimeSpan interval = TimeSpan.Zero;

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
            double intervalSeconds;
            if (string.IsNullOrWhiteSpace(intervalTextBox.Text.Trim()))
               interval = TimeSpan.Zero;
            else if (double.TryParse(intervalTextBox.Text.Trim(), out intervalSeconds) && intervalSeconds >= 0)
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

      TargetCoordinateUnits GetSelectedMode()
      {
         TargetCoordinateUnits mode = TargetCoordinateUnits.None;

         if (targetModeComboBox == null)
            return mode;

         var setting = targetModeComboBox.SelectedValue as string;
         if (setting == null)
            return mode;

         Enum.TryParse(setting, out mode);
         return mode;
      }

      Point GetLocationForMode(TargetCoordinateUnits units)
      {
         switch (units)
         {
            case TargetCoordinateUnits.AbsoluteTile:
               return new Point(absoluteTileXUpDown.Value, absoluteTileYUpDown.Value);

            case TargetCoordinateUnits.AbsoluteXY:
               return new Point(absoluteXUpDown.Value, absoluteYUpDown.Value);

            case TargetCoordinateUnits.RelativeTile:
               return new Point((int)relativeTileXComboBox.SelectedValue, (int)relativeTileYComboBox.SelectedValue);

            case TargetCoordinateUnits.RelativeXY:
               return new Point(relativeXUpDown.Value, relativeYUpDown.Value);

            case TargetCoordinateUnits.RelativeRadius:
               goto case TargetCoordinateUnits.RelativeTile;

            case TargetCoordinateUnits.AbsoluteRadius:
               goto case TargetCoordinateUnits.AbsoluteTile;

            default:
               return new Point(0, 0);
         }
      }

      void SetTargetForMode(SpellTarget target)
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

            case TargetCoordinateUnits.RelativeXY:
               relativeXUpDown.Value = target.Location.X;
               relativeYUpDown.Value = target.Location.Y;
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

      void ToggleTargetMode(TargetCoordinateUnits units)
      {
         var requiresTarget = units != TargetCoordinateUnits.None;
         var isSelfTarget = units == TargetCoordinateUnits.Self;
         var isRadius = units == TargetCoordinateUnits.AbsoluteRadius || units == TargetCoordinateUnits.RelativeRadius;

         if (characterComboBox != null)
            characterComboBox.Visibility = (units == TargetCoordinateUnits.Character) ? Visibility.Visible : Visibility.Collapsed;

         if (relativeTileXComboBox != null)
            relativeTileXComboBox.Visibility = (units == TargetCoordinateUnits.RelativeTile || units == TargetCoordinateUnits.RelativeRadius) ? Visibility.Visible : Visibility.Collapsed;

         if (relativeXUpDown != null)
            relativeXUpDown.Visibility = (units == TargetCoordinateUnits.RelativeXY) ? Visibility.Visible : Visibility.Collapsed;

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

         var height = 300;

         if (requiresTarget)
            height += 40;

         if (isRadius)
            height += 90;

         if (!isSelfTarget && requiresTarget)
            height += 40;

         this.Height = height;
      }

      void targetModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (e.AddedItems.Count < 1)
         {
            ToggleTargetMode(TargetCoordinateUnits.None);
            return;
         }

         var item = e.AddedItems[0] as UserSetting;
         if (item == null)
         {
            ToggleTargetMode(TargetCoordinateUnits.None);
            return;
         }

         TargetCoordinateUnits mode;

         if (!Enum.TryParse<TargetCoordinateUnits>(item.Value as string, out mode))
            mode = TargetCoordinateUnits.None;

         ToggleTargetMode(mode);
      }

      void okButton_Click(object sender, RoutedEventArgs e)
      {
         if (!ValidateFlowerTarget())
            return;

         this.DialogResult = true;
         this.Close();
      }
   }
}
