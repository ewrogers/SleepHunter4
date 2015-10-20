using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SleepHunter.Updates
{
   public static class BuildHelper
   {
      static readonly Regex BuildTimeRegex = new Regex(@"\s*(?<year>[0-9]{2})(?<month>[0-9A-Fa-f]{1})(?<day>[0-9]{2})(?<revision>[0-9A-Fa-f]?)\s*");

      public static bool TryGetBuildTime(this string build, out DateTime date, out int revision)
      {
         date = DateTime.MinValue;
         revision = 0;
         var match = BuildTimeRegex.Match(build);

         if (!match.Success)
            return false;

         int year;
         int month;
         int day;

         if (!int.TryParse(match.Groups["year"].Value, out year))
            return false;

         if (!int.TryParse(match.Groups["month"].Value, NumberStyles.HexNumber, null, out month))
            return false;

         if (!int.TryParse(match.Groups["day"].Value, out day))
            return false;

         if (!int.TryParse(match.Groups["revision"].Value, NumberStyles.HexNumber, null, out revision))
            return false;

         date = new DateTime(year + 2000, month, day);
         return true;
      }

      public static bool TryCompareBuild(string buildA, string buildB, out int result)
      {
         result = -2;

         DateTime buildTimestampA, buildTimestampB;
         int buildRevisionA, buildRevisionB;

         if (!TryGetBuildTime(buildA, out buildTimestampA, out buildRevisionA))
            return false;

         if (!TryGetBuildTime(buildB, out buildTimestampB, out buildRevisionB))
            return false;

         if (buildTimestampA > buildTimestampB)
            result = 1;
         else if (buildTimestampB > buildTimestampA)
            result = -1;
         else if (buildTimestampA == buildTimestampB)
         {
            if (buildRevisionA > buildRevisionB)
               result = 1;
            else if (buildRevisionB > buildRevisionA)
               result = -1;
            else
               result = 0;
         }

         return true;
      }

   }
}
