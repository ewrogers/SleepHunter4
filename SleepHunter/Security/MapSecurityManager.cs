using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SleepHunter.Security
{
   public sealed class MapSecurityManager
   {
      static readonly string DefaultVerifierTypeName = "SleepHunter.Security.DefaultMapVerifier";

      #region Singleton
      static readonly Assembly dynamicAssembly;
      static readonly Type verifierType;

      static readonly string default64 = @"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";
      static IMapVerifier verifier;

      public static IMapVerifier Verifier { get { return verifier; } }

      private MapSecurityManager()
      {
         
      }

      static MapSecurityManager()
      {
         try
         {
            var result = SecurityCompiler.SecureCompile(default64);

            if (result.Errors.Count > 0)
               return;

            var loadedAssembly = result.CompiledAssembly;
            var defaultVerifierType = loadedAssembly.GetType(DefaultVerifierTypeName, false, true);

            if (defaultVerifierType == null)
               return;

            var defaultVerifier = Activator.CreateInstance(defaultVerifierType) as IMapVerifier;

            if (defaultVerifier == null)
               return;

            dynamicAssembly = loadedAssembly;
            verifierType = defaultVerifierType;
            verifier = defaultVerifier as IMapVerifier;
         }
         catch { }
         finally
         {
            if (dynamicAssembly == null)
               verifier = new NullMapSecurityManager();
         }
      }
      #endregion

      public static bool IsDynamicAssemblyLoaded
      {
         get
         {
#if DEBUG
            return true;
#else
            return dynamicAssembly != null;
#endif
         }
      }

      public int Count
      {
         get { return verifier.Count; }
      }

      public IEnumerable<MapSecurityInformation> Maps
      {
         get { return verifier.Maps; }
      }

      public void AddMap(MapSecurityInformation map)
      {
         verifier.AddMap(map);
      }

      public bool ContainsMap(int mapNumber)
      {
         return verifier.ContainsMap(mapNumber);
      }

      public bool IsMapAllowed(string mapHash)
      {
         return verifier.IsMapAllowed(mapHash);
      }

      public MapSecurityInformation GetMap(int mapNumber)
      {
         return verifier.GetMap(mapNumber);
      }

      public bool RemoveMap(int mapNumber)
      {
         return verifier.RemoveMap(mapNumber);
      }

      public void ClearMaps()
      {
         verifier.ClearMaps();
      }

      public string ComputeMapHash(int mapNumber, string mapName)
      {
         return verifier.ComputeMapHash(mapNumber, mapName);
      }

      [Conditional("DEBUG")]
      public static void SetVerifier(IMapVerifier verifier)
      {
         MapSecurityManager.verifier = verifier;
      }
   }
}
