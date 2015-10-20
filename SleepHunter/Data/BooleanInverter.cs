using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SleepHunter.Data
{
   public sealed class BooleanInverter: IValueConverter
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
