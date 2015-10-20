using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SleepHunter
{
   public static class AssemblyExtender
   {
      public static T GetCustomAttribute<T>(this Assembly assembly) where T : Attribute
      {
         return Attribute.GetCustomAttribute(assembly, typeof(T)) as T;
      }
   }
}
