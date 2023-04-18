using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SleepHunter.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public const string ReverseParameter = "Reverse";
        public const string CollapseParameter = "Collapse";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;
            var isReversed = string.Equals(ReverseParameter, parameterString, StringComparison.OrdinalIgnoreCase);

            if (value is string stringValue)
                return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Collapsed : Visibility.Visible;

            if (value is Visibility visibility)
            {
                if (isReversed)
                    return visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                else
                    return visibility;
            }

            var boolean = (bool)value;

            if (isReversed)
                boolean = !boolean;

            if (boolean)
                return Visibility.Visible;
            else
            {
                if (string.Equals(CollapseParameter, parameterString, StringComparison.OrdinalIgnoreCase))
                    return Visibility.Collapsed;
                else
                    return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
                return true;
            else
                return false;
        }
    }
}
