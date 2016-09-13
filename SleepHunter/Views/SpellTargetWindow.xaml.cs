using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
    SpellQueueItem spellQueueItem = new SpellQueueItem();

    public SpellQueueItem SpellQueueItem
    {
      get { return spellQueueItem; }
      private set { spellQueueItem = value; }
    }

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

    public static readonly DependencyProperty IsEditModeProperty =
        DependencyProperty.Register("IsEditMode", typeof(bool), typeof(SpellTargetWindow), new PropertyMetadata(false));

    public static readonly DependencyProperty SpellProperty =
        DependencyProperty.Register("Spell", typeof(Spell), typeof(SpellTargetWindow), new PropertyMetadata(null));

    public SpellTargetWindow(Spell spell, SpellQueueItem item, bool isEditMode = true)
       : this(spell)
    {
      if (isEditMode)
      {
        this.Title = "Edit Target";
        okButton.Content = "_Save Changes";
      }

      this.SpellQueueItem.Id = item.Id;
      SetTargetForMode(item.Target);

      maxLevelCheckBox.IsChecked = item.HasTargetLevel;

      if (item.HasTargetLevel)
        maxLevelUpDown.Value = item.TargetLevel.Value;

      this.IsEditMode = isEditMode;
    }

    public SpellTargetWindow(Spell spell)
       : this()
    {
      this.Spell = spell;

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
        WarningBorder.Visibility = Visibility.Visible;

        var opacityAnimation = new DoubleAnimation(1.0, 0.25, new Duration(TimeSpan.FromSeconds(0.4)));
        opacityAnimation.AccelerationRatio = 0.75;
        opacityAnimation.AutoReverse = true;
        opacityAnimation.RepeatBehavior = RepeatBehavior.Forever;

        WarningIcon.BeginAnimation(FrameworkElement.OpacityProperty, opacityAnimation);
      }
    }

    public SpellTargetWindow()
    {
      InitializeComponent();
      InitializeViews();

      ToggleTargetMode(TargetCoordinateUnits.None);
      WarningBorder.Visibility = Visibility.Collapsed;
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
      if (player == null)
        return;

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

    bool ValidateSpellTarget()
    {
      #region Spell Check
      if (this.Spell == null)
      {
        this.ShowMessageBox("Invalid Spell",
           "This spell is no longer valid.",
           "This spell window will now close, please try again.",
           MessageBoxButton.OK);

        this.Close();
        return false;
      }
      #endregion

      var selectedMode = GetSelectedMode();

      #region Check Target Mode
      if (this.Spell.TargetMode == SpellTargetMode.Target && selectedMode == TargetCoordinateUnits.None)
      {
        this.ShowMessageBox("Target Required",
           "This spell requires a target.",
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

      spellQueueItem.Icon = this.Spell.Icon;
      spellQueueItem.Name = this.Spell.Name;
      spellQueueItem.CurrentLevel = this.Spell.CurrentLevel;
      spellQueueItem.MaximumLevel = this.Spell.MaximumLevel;

      if (!this.IsEditMode)
        spellQueueItem.StartingLevel = this.Spell.CurrentLevel;

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

      var height = 330;

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
      if (!ValidateSpellTarget())
        return;

      this.DialogResult = true;
      this.Close();
    }
  }
}
