using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SleepHunter.Security
{
   public sealed class MapSecurityInformation
   {
      readonly int mapNumber;
      readonly string mapName;
      readonly string mapHash;

      public int MapNumber { get { return mapNumber; } }
      public string MapName { get { return mapName; } }
      public string MapHash { get { return mapHash; } }

      public MapSecurityInformation(int mapNumber, string mapName, string mapHash)
      {
         this.mapNumber = mapNumber;
         this.mapName = mapName;
         this.mapHash = mapHash;
      }

      public override string ToString()
      {
         return this.MapName;
      }
   }
}
