using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using SleepHunter.Extensions;

namespace SleepHunter.Controls
{
    public partial class NumericUpDown : UserControl
   {
      new public Brush BorderBrush
      {
         get { return base.BorderBrush; }
         set { base.BorderBrush = value; }
      }

      public double Value
      {
         get { return (double)GetValue(ValueProperty); }
         set { SetValue(ValueProperty, value); }
      }

      public double SmallIncrement
      {
         get { return (double)GetValue(SmallIncrementProperty); }
         set { SetValue(SmallIncrementProperty, value); }
      }

      public double LargeIncrement
      {
         get { return (double)GetValue(LargeIncrementProperty); }
         set { SetValue(LargeIncrementProperty, value); }
      }

      public bool IsHexadecimal
      {
         get { return (bool)GetValue(IsHexadecimalProperty); }
         set { SetValue(IsHexadecimalProperty, value); }
      }

      public int DecimalPlaces
      {
         get { return (int)GetValue(DecimalPlacesProperty); }
         set { SetValue(DecimalPlacesProperty, value); }
      }

      public double Minimum
      {
         get { return (double)GetValue(MinimumProperty); }
         set { SetValue(MinimumProperty, value); }
      }

      public double Maximum
      {
         get { return (double)GetValue(MaximumProperty); }
         set { SetValue(MaximumProperty, value); }
      }

      public bool AllowTextInput
      {
         get { return (bool)GetValue(AllowTextInputProperty); }
         set { SetValue(AllowTextInputProperty, value); }
      }

      public static readonly DependencyProperty AllowTextInputProperty =
          DependencyProperty.Register("AllowTextInput", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(true));

      public static readonly DependencyProperty MaximumProperty =
          DependencyProperty.Register("Maximum", typeof(double), typeof(NumericUpDown), new PropertyMetadata(100.0));
      
      public static readonly DependencyProperty MinimumProperty =
          DependencyProperty.Register("Minimum", typeof(double), typeof(NumericUpDown), new PropertyMetadata(0.0));

      public static readonly DependencyProperty IsHexadecimalProperty =
          DependencyProperty.Register("IsHexadecimal", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

      public static readonly DependencyProperty DecimalPlacesProperty =
         DependencyProperty.Register("DecimalPlaces", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));

      public static readonly DependencyProperty LargeIncrementProperty =
          DependencyProperty.Register("LargeIncrement", typeof(double), typeof(NumericUpDown), new PropertyMetadata(5.0));
      
      public static readonly DependencyProperty SmallIncrementProperty =
          DependencyProperty.Register("SmallIncrement", typeof(double), typeof(NumericUpDown), new PropertyMetadata(1.0));      

      public static readonly DependencyProperty ValueProperty =
          DependencyProperty.Register("Value", typeof(double), typeof(NumericUpDown), new PropertyMetadata(0.0));

      public NumericUpDown()
      {
         InitializeComponent();
      }

      public void SelectAll()
      {
         PART_Value.Focus();
         PART_Value.SelectAll();
      }

      void incrementButton_Click(object sender, RoutedEventArgs e)
      {
         this.Value = Math.Min(this.Maximum, this.Value + this.SmallIncrement);
      }

      void decrementButton_Click(object sender, RoutedEventArgs e)
      {
         this.Value = Math.Max(this.Minimum, this.Value - this.SmallIncrement);
      }

      void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
      {
         var textBox = sender as TextBox;
         if (textBox == null) return;

         var currentText = textBox.Text;
         var isValid = true;
         
         foreach (char c in e.Text)
         {
            if (c == '\r' || c == '\n')
            {
               isValid = false;
               break;
            }

            if (this.IsHexadecimal)
               isValid = c.IsValidHexDigit(allowControl: true);
            else
               isValid = c.IsValidDecimalCharacter(allowControl: true) || (this.DecimalPlaces > 0 && c == '.') || c == '-';

            if (!isValid)
               break;
         }

         e.Handled = !isValid;
      }

      void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
      {
         var delta = e.Delta;

         if (delta > 0)
            this.Value = Math.Min(this.Maximum, this.Value + this.LargeIncrement);
         else if (delta < 0)
            this.Value = Math.Max(this.Minimum, this.Value - this.LargeIncrement);

         UpdateValue();
      }

      void TextBox_LostFocus(object sender, RoutedEventArgs e)
      {
         UpdateValue();
      }

      void TextBox_TextChanged(object sender, TextChangedEventArgs e)
      {
         UpdateValue();
      }

      void UpdateValue()
      {
         var binding = BindingOperations.GetBindingExpression(PART_Value, TextBox.TextProperty);

         if (binding != null)
            binding.UpdateSource();

         this.Value = Math.Max(this.Minimum, Math.Min(this.Maximum, this.Value));
      }
   }
}
