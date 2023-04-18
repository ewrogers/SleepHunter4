using System;
using System.Globalization;
using System.Windows.Data;

using SleepHunter.Extensions;

namespace SleepHunter.Converters
{
    public sealed class TimeSpanConverter : IValueConverter
    {
        public const string ShortFormatParameter = "Short";
        public const string ShortNoSecondaryFormatParameter = "ShortNoSecondary";
        public const string LongFormatParameter = "Long";
        public const string LongNoSecondaryFormatParameter = "LongNoSecondary";
        public const string FractionalFormatParameter = "Fractional";
        public const string FractionalShortFormatParameter = "FractionalShort";
        public const string CooldownFormatParameter = "Cooldown";
        public const string SecondsFormatParameter = "Seconds";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan timeSpan;

            if (value == null)
                return null;

            if (value is TimeSpan?)
            {
                var nullableTimeSpan = value as TimeSpan?;

                if (!nullableTimeSpan.HasValue)
                    return null;

                timeSpan = nullableTimeSpan.Value;
            }
            else
            {
                timeSpan = (TimeSpan)value;
            }

            var format = parameter as string;
            var useShortFormat = false;
            var showSecondary = true;

            if (string.Equals(format, ShortFormatParameter, StringComparison.OrdinalIgnoreCase))
                useShortFormat = true;

            if (string.Equals(format, ShortNoSecondaryFormatParameter, StringComparison.OrdinalIgnoreCase))
            {
                useShortFormat = true;
                showSecondary = false;
            }

            if (string.Equals(format, LongFormatParameter, StringComparison.OrdinalIgnoreCase))
                useShortFormat = false;

            if (string.Equals(format, LongNoSecondaryFormatParameter, StringComparison.OrdinalIgnoreCase))
            {
                useShortFormat = false;
                showSecondary = false;
            }

            if (string.Equals(format, FractionalFormatParameter, StringComparison.OrdinalIgnoreCase))
            {
                return timeSpan.ToFractionalEnglish();
            }

            if (string.Equals(format, FractionalShortFormatParameter, StringComparison.OrdinalIgnoreCase))
            {
                return timeSpan.ToFractionalEnglish(true);
            }

            if (string.Equals(format, CooldownFormatParameter, StringComparison.OrdinalIgnoreCase))
            {
                if (timeSpan.Hours > 0)
                    return $"{timeSpan.Hours}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
                else if (timeSpan.Minutes > 0)
                    return $"{timeSpan.Minutes}:{timeSpan.Seconds:00}";
                else
                    return $"{timeSpan.TotalSeconds:0.0}";
            }

            if (string.Equals(format, SecondsFormatParameter, StringComparison.OrdinalIgnoreCase))
            {
                return timeSpan.TotalSeconds.ToPluralString(" seconds", " second", "0.#");
            }

            if (useShortFormat)
                return timeSpan.ToShortEnglish(showSecondary);
            else
                return timeSpan.ToLongEnglish(showSecondary);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
