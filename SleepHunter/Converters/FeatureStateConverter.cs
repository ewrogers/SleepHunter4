using System;
using System.Globalization;
using System.Windows.Data;

namespace SleepHunter.Converters
{
    public sealed class FeatureStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new ArgumentException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new ArgumentException();
        }
    }
}
