using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using SleepHunter.Settings;

namespace SleepHunter.Converters
{
    public sealed class FeatureVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ClientVersion version)
                throw new ArgumentException("Value must be a ClientVersion instance", nameof(value));

            if (parameter is not string flagKey)
                throw new ArgumentException("Feature flag key is required as a parameter", nameof(parameter));

            if (version == null || string.IsNullOrWhiteSpace(flagKey))
                return Visibility.Collapsed;

            var flag = version.Features.FirstOrDefault(flag => flag.IsEnabled && string.Equals(flagKey, flag.Key, StringComparison.OrdinalIgnoreCase));
            return flag != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
