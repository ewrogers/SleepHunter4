using System;
using System.Windows.Data;

namespace SleepHunter.Converters
{
    internal sealed class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var boolean = (bool)value;
            return !boolean;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var boolean = (bool)value;
            return !boolean;
        }
    }
}
