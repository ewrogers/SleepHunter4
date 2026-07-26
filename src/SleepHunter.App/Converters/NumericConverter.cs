using System;
using System.Globalization;
using System.Windows.Data;

namespace SleepHunter.Converters
{
    public sealed class NumericConverter : IValueConverter
    {
        private const int ThousandsThreshold = 10_000;
        private const int MillionsThreshold = 1_000_000;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isHexadecimal = string.Equals("Hexadecimal", parameter as string, StringComparison.OrdinalIgnoreCase);
            var isThousands = string.Equals("Thousands", parameter as string, StringComparison.OrdinalIgnoreCase);

            var integerValue = System.Convert.ToInt64(value, CultureInfo.InvariantCulture);

            if (isHexadecimal)
            {
                return integerValue.ToString("X");
            }

            if (isThousands && integerValue > ThousandsThreshold)
            {
                if (integerValue >= MillionsThreshold)
                {
                    var fractionalMillions = integerValue / 1_000_000.0;
                    return $"{fractionalMillions:0.0}m";
                }
                else if (integerValue >= ThousandsThreshold)
                {
                    var fractionalThousands = integerValue / 1_000.0;
                    return $"{fractionalThousands:0}k";
                }
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isHexadecimal = string.Equals("Hexadecimal", parameter as string, StringComparison.OrdinalIgnoreCase);
            var isThousands = string.Equals("Thousands", parameter as string, StringComparison.OrdinalIgnoreCase);

            var valueString = value as string;

            if (isHexadecimal)
            {
                if (!uint.TryParse(valueString, NumberStyles.HexNumber, null, out var hexValue))
                    return 0;
                else
                    return hexValue;
            }

            if (isThousands)
            {
                if (valueString.EndsWith("m"))
                {
                    var trimmedString = valueString.TrimEnd('m');
                    if (!double.TryParse(trimmedString, NumberStyles.Float, null, out var doubleValue))
                        return 0;
                    else
                        return (int)Math.Round(doubleValue * 1_000_000.0);
                }
                else if (valueString.EndsWith("k"))
                {
                    var trimmedString = valueString.TrimEnd('m');
                    if (!double.TryParse(trimmedString, NumberStyles.Float, null, out var doubleValue))
                        return 0;
                    else
                        return (int)Math.Round(doubleValue * 1_000.0);
                }
            }

            if (!double.TryParse(valueString, out var decValue))
                return 0;
            else
                return decValue;
        }
    }
}
