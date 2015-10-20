using System;
using System.Collections.Generic;
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

using SleepHunter.Metadata;

namespace SleepHunter
{
   public partial class LineModifiersEditorWindow : Window
   {
      SpellLineModifiers modifiers = new SpellLineModifiers();

      public SpellLineModifiers Modifiers
      {
         get { return modifiers; }
         private set { modifiers = value; }
      }

      public bool IsEditMode
      {
         get { return (bool)GetValue(IsEditModeProperty); }
         set { SetValue(IsEditModeProperty, value); }
      }

      public static readonly DependencyProperty IsEditModeProperty =
          DependencyProperty.Register("IsEditMode", typeof(bool), typeof(LineModifiersEditorWindow), new PropertyMetadata(false));

      public LineModifiersEditorWindow(SpellLineModifiers modifiers, bool isEditMode = true)
         :this()
      {
         actionComboBox.SelectedItem = modifiers.Action;
         scopeComboBox.SelectedItem = modifiers.Scope;
         scopeNameTextBox.Text = modifiers.ScopeName;
         valueUpDown.Value = modifiers.Value;
         minThresholdUpDown.Value = modifiers.MinThreshold;
         maxThresholdUpDown.Value = modifiers.MaxThreshold;

         this.IsEditMode = isEditMode;

         if (this.IsEditMode)
            this.Title = "Edit Modifiers";
      }
      
      public LineModifiersEditorWindow()
      {
         InitializeComponent();

         this.Title = "Add Modifiers";
      }

      void Window_Loaded(object sender, RoutedEventArgs e)
      {
         actionComboBox.Focus();
      }

      bool ValidateModifiers()
      {
         var minThreshold = minThresholdUpDown.Value;
         var maxThreshold = maxThresholdUpDown.Value;

         if (actionComboBox.SelectedIndex < 0)
         {
            this.ShowMessageBox("Invalid Action",
               "Line modifiers must have an action type specified.",
               "Please select one of the options available.",
               MessageBoxButton.OK);

            actionComboBox.Focus();
            actionComboBox.IsDropDownOpen = true;
            return false;
         }

         if (scopeComboBox.SelectedIndex < 0)
         {
            this.ShowMessageBox("Invalid Scope",
               "Line modifiers must have a scope type specified.",
               "Please select one of the options available.",
               MessageBoxButton.OK);

            scopeComboBox.Focus();
            scopeComboBox.IsDropDownOpen = true;
            return false;
         }

         modifiers.Action = (ModifierAction)actionComboBox.SelectedItem;
         modifiers.Scope = (ModifierScope)scopeComboBox.SelectedItem;
         modifiers.ScopeName = scopeNameTextBox.Text.Trim();

         if (modifiers.Scope != ModifierScope.All && string.IsNullOrWhiteSpace(modifiers.ScopeName))
         {
            this.ShowMessageBox("Invalid Scope Name",
               "Scope name cannot be null or empty.",
               "Please specify a valid group or spell name.",
               MessageBoxButton.OK);

            scopeNameTextBox.Focus();
            scopeNameTextBox.SelectAll();
            return false;
         }

         modifiers.Value = (int)valueUpDown.Value;
         modifiers.MinThreshold = (int)minThreshold;
         modifiers.MaxThreshold = (int)maxThreshold;
         return true;
      }

      void okButton_Click(object sender, RoutedEventArgs e)
      {
         if (!ValidateModifiers())
            return;

         this.DialogResult = true;
         this.Close();
      }

      void cancelButton_Click(object sender, RoutedEventArgs e)
      {
         this.DialogResult = false;
         this.Close();
      }

      void scopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (e.AddedItems.Count < 1)
         {
            if (scopeNameTextBox != null)
               scopeNameTextBox.IsEnabled = true;
         }
         else
         {
            var scope = (ModifierScope)e.AddedItems[0];

            if (scopeNameTextBox != null)
               scopeNameTextBox.IsEnabled = (scope != ModifierScope.All);
         }
      }
   }
}
