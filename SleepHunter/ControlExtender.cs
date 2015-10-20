using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SleepHunter
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
