using System;
using System.Globalization;
using System.Windows.Data;

using SleepHunter.Extensions;

namespace SleepHunter.Converters
{
  public sealed class TimeSpanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      TimeSpan timeSpan;

      if (value == null)
        return null;

      if (value is Nullable<TimeSpan>)
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

      string format = parameter as string;
      bool useShortFormat = false;
      bool showSecondary = true;

      if (string.Equals(format, "short", StringComparison.OrdinalIgnoreCase))
        useShortFormat = true;

      if (string.Equals(format, "shortNoSecondary", StringComparison.OrdinalIgnoreCase))
      {
        useShortFormat = true;
        showSecondary = false;
      }

      if (string.Equals(format, "long", StringComparison.OrdinalIgnoreCase))
        useShortFormat = false;

      if (string.Equals(format, "longNoSecondary", StringComparison.OrdinalIgnoreCase))
      {
        useShortFormat = false;
        showSecondary = false;
      }

      if (string.Equals(format, "fractional", StringComparison.OrdinalIgnoreCase))
      {
        return timeSpan.ToFractionalEnglish();
      }

      if (string.Equals(format, "fractionalShort", StringComparison.OrdinalIgnoreCase))
      {
        return timeSpan.ToFractionalEnglish(true);
      }

      if (string.Equals(format, "cooldown", StringComparison.OrdinalIgnoreCase))
      {
        return timeSpan.ToFractionalEnglish(true, "0.0");
      }

      if (string.Equals(format, "seconds", StringComparison.OrdinalIgnoreCase))
      {
        return timeSpan.TotalSeconds.ToPluralString(" seconds", " second", "0.#");
      }

      if (useShortFormat)
        return timeSpan.ToShortEnglish(showSecondary);
      else
        return timeSpan.ToLongEnglish(showSecondary);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
