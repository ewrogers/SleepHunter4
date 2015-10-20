using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SleepHunter.Data
{
   public sealed class NumericConverter : IValueConverter
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
            uint hexValue;
            if (!uint.TryParse(valueString, NumberStyles.HexNumber, null, out hexValue))
               return 0;
            else
               return hexValue;
         }

         double decValue;
         if (!double.TryParse(valueString, out decValue))
            return 0;
         else
            return decValue;

      }
   }
}
