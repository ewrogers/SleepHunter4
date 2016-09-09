using System;
using System.Windows.Data;

namespace SleepHunter.Converters
{
    public sealed class LessThanConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         var integer = value is double ? (double)value : (int)value;
         var threshold = 0.0;

         if (!double.TryParse(parameter as string, out threshold))
            return false;

         return integer < threshold;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public sealed class LessThanOrEqualConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         var integer = value is double ? (double)value : (int)value;
         var threshold = 0.0;

         if (!double.TryParse(parameter as string, out threshold))
            return false;

         return integer <= threshold;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public sealed class GreaterThanConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         var integer = value is double ? (double)value : (int)value;
         var threshold = 0.0;

         if (!double.TryParse(parameter as string, out threshold))
            return false;

         return integer > threshold;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public sealed class GreaterThanOrEqualConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         var integer = value is double ? (double)value : (int)value;
         var threshold = 0.0;

         if (!double.TryParse(parameter as string, out threshold))
            return false;

         return integer >= threshold;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }

   public sealed class AbsoluteValueConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         var integer = value is double ? (double)value : (int)value;

         return Math.Abs(integer);
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }
}
