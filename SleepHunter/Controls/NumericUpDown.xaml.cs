using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SleepHunter.Controls
{
    public partial class NumericUpDown : UserControl
    {
        private static readonly Regex UnsignedIntegerRegex = new Regex(@"^\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UnsignedDoubleRegex = new Regex(@"^\d+(\.\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SignedIntegerRegex = new Regex(@"^-?\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SignedDoubleRegex = new Regex(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HexadecimalRegex = new Regex(@"^(0x)?[0-9a-f]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly DependencyProperty DecoratorTextProperty =
            DependencyProperty.Register("DecoratorText", typeof(string), typeof(NumericUpDown), new PropertyMetadata(string.Empty));

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

        public new Brush BorderBrush
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

        public string DecoratorText
        {
            get { return (string)GetValue(DecoratorTextProperty); }
            set { SetValue(DecoratorTextProperty, value); }
        }

        public NumericUpDown() => InitializeComponent();

        public void SelectAll()
        {
            PART_Value.Focus();
            PART_Value.SelectAll();
        }

        private void incrementButton_Click(object sender, RoutedEventArgs e) => Value = Math.Min(Maximum, Value + SmallIncrement);

        private void decrementButton_Click(object sender, RoutedEventArgs e) => Value = Math.Max(Minimum, Value - SmallIncrement);

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
                return;

            var newText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);

            var allowNegative = Minimum < 0;
            if (newText == "-" && allowNegative)
                return;

            if (IsHexadecimal) {
                e.Handled = !HexadecimalRegex.IsMatch(newText);
            }
            else if (DecimalPlaces > 0)
            {
                Match match;

                // If allow negative, use signed, otherwise use unsigned pattern
                if (allowNegative)
                    match = SignedDoubleRegex.Match(newText);
                else 
                    match = UnsignedDoubleRegex.Match(newText);

                if (!match.Success)
                {
                    e.Handled = true;
                    return;
                }

                // If more decimal places than allowed, reject
                var valueDecimalPlaces = match.Groups[1].Length - 1;
                if (valueDecimalPlaces > DecimalPlaces)
                {
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                // If allow negative, use signed otherwise use unsigned pattern
                if (allowNegative)
                    e.Handled = !SignedIntegerRegex.IsMatch(newText);
                else
                    e.Handled = !UnsignedIntegerRegex.IsMatch(newText);
            }
        }

        private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var delta = e.Delta;

            if (delta > 0)
                Value = Math.Min(Maximum, Value + LargeIncrement);
            else if (delta < 0)
                Value = Math.Max(Minimum, Value - LargeIncrement);

            UpdateValue();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox))
                return;

            if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text == "-")
                textBox.Text = IsHexadecimal ? "0x0" : "0";

            UpdateValue();
        }

        private void UpdateValue()
        {
            var binding = BindingOperations.GetBindingExpression(PART_Value, TextBox.TextProperty);
            binding?.UpdateSource();

            Value = Math.Max(Minimum, Math.Min(Maximum, Value));
        }
    }
}
