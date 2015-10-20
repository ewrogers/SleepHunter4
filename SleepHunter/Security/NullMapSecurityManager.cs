using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Security
{
   internal sealed class NullMapSecurityManager : IMapVerifier
   {
      public int Count { get { return 0; } }

      public IEnumerable<MapSecurityInformation> Maps { get { yield break; } }

      public void AddMap(MapSecurityInformation map) { }

      public bool ContainsMap(int mapNumber) { return false; }

      public bool IsMapAllowed(string mapHash)
      {
#if DEBUG
         return true;
#else
         return false;
#endif
      }

      public MapSecurityInformation GetMap(int mapNumber) { return null; }

      public bool RemoveMap(int mapNumber) { return false; }

      public void ClearMaps() { }

      public string ComputeMapHash(int mapNumber, string mapName) { return null; }
   }
}
