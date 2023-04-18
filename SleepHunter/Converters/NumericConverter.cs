﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace SleepHunter.Converters
{
    internal sealed class NumericConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isHexadecimal = string.Equals("Hexadecimal", parameter as string, StringComparison.OrdinalIgnoreCase);

            if (isHexadecimal)
            {
                var hexValue = (uint)value;
                return hexValue.ToString("X");
            }

            var decValue = (double)value;
            return decValue.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isHexadecimal = string.Equals("Hexadecimal", parameter as string, StringComparison.OrdinalIgnoreCase);
            var valueString = value as string;

            if (isHexadecimal)
            {
                if (!uint.TryParse(valueString, NumberStyles.HexNumber, null, out var hexValue))
                    return 0;
                else
                    return hexValue;
            }

            if (!double.TryParse(valueString, out var decValue))
                return 0;
            else
                return decValue;

        }
    }
}
