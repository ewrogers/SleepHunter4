using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SleepHunter.Updates
{
   public sealed class VersionExtender
   {
      static readonly Regex VersionRegex = new Regex(@"\s*(?<major>\d{1,})\.(?<minor>\d{1,})\.?(?<build>\d*)\.?(?<revision>\d*)\s*");

      public static bool TryParse(string versionString, out Version version)
      {
         version = null;

         var match = VersionRegex.Match(versionString);

         if (!match.Success)
            return false;

         int major, minor, revision = 0, build = 0;

         if (!int.TryParse(match.Groups["major"].Value, out major))
            return false;

         if (!int.TryParse(match.Groups["minor"].Value, out minor))
            return false;

         if (match.Groups["build"].Success)
            if (!int.TryParse(match.Groups["build"].Value, out build))
               return false;

         if (match.Groups["revision"].Success)
            if (!int.TryParse(match.Groups["revision"].Value, out revision))
               return false;
        
         version = new Version(major, minor, build, revision);
         return true;
      }

      public static void GetCurrentVersionInfo(out Version version, out string build, out bool isDebug)
      {
         var assembly = Assembly.GetExecutingAssembly();
         var assemblyName = assembly.GetName();

         version = assemblyName.Version;

         var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
         var configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();

         build = informationalVersion.InformationalVersion;
         isDebug = string.Equals(configuration.Configuration, "Debug", StringComparison.OrdinalIgnoreCase);

         return;
      }
   }
}
