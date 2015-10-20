using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Security
{
   public interface IMapVerifier
   {
      int Count { get; }
      IEnumerable<MapSecurityInformation> Maps { get; }

      void AddMap(MapSecurityInformation map);
      bool ContainsMap(int mapNumber);
      bool IsMapAllowed(string mapHash);
      MapSecurityInformation GetMap(int mapNumber);
      bool RemoveMap(int mapNumber);
      void ClearMaps();

      string ComputeMapHash(int mapNumber, string mapName);
   }
}
