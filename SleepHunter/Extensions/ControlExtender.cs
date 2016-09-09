using System;
using System.Windows.Controls;

namespace SleepHunter.Extensions
{
    public static class ControlExtender
   {
      public static T FindItem<T>(this ItemsControl control, Func<T, bool> selector) where T : class
      {
         if (control == null)
            throw new ArgumentNullException("control");

         if (selector == null)
            throw new ArgumentNullException("selector");

         foreach(T item in control.Items)
         {
           var isMatch = selector(item);

            if (isMatch)
               return item;
         }

         return null;
      }

      public static T FindItemOrDefault<T>(this ItemsControl control, Func<T, bool> selector, T defaultValue = default(T)) where T : class
      {
         var value = FindItem(control, selector);

         if (value == null)
            return defaultValue;
         else
            return value;
      }
   }
}
