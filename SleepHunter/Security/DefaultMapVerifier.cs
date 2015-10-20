using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SleepHunter.Security
{
   public sealed class DefaultMapVerifier : IMapVerifier
   {
      static readonly uint MapNumberSaltEven = 0xDEAD;
      static readonly uint MapNumberSaltOdd = 0xBEEF;

      ConcurrentDictionary<string, string> hashCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      ConcurrentDictionary<int, MapSecurityInformation> maps = new ConcurrentDictionary<int, MapSecurityInformation>();

      public DefaultMapVerifier()
      {
         // Mileth
         AddVerifiedMap(600, "East Woodland Crossroads");
         AddVerifiedMap(622, "Enchanted Garden");
         AddVerifiedMap(425, "Crypt Vestibule");
         AddVerifiedMap(1, "Crypt 1-1");
         AddVerifiedMap(2, "Crypt 2-1");
         AddVerifiedMap(623, "Wasteland");
         AddVerifiedMap(5260, "Training Dojo 1");
         AddVerifiedMap(5261, "Training Dojo 2");
         AddVerifiedMap(5262, "Training Dojo 3");
         AddVerifiedMap(5263, "Training Dojo 4");
         AddVerifiedMap(5264, "Training Dojo 5");
         AddVerifiedMap(5265, "Training Dojo 6");
         AddVerifiedMap(5266, "Training Dojo 7");
         AddVerifiedMap(5267, "Training Dojo 8");
         AddVerifiedMap(5268, "Training Dojo 9");
         AddVerifiedMap(393, "Path Reception");

         // Abel
         AddVerifiedMap(3014, "Abel Port Way");
         AddVerifiedMap(502, "Abel Port");
         AddVerifiedMap(185, "Coast");

         // West Woodlands
         AddVerifiedMap(449, "West Woodlands 8-1");

         // Kas Mines
         AddVerifiedMap(660, "Mine Entrance");

         // Piet
         AddVerifiedMap(3020, "Piet Village Way");
         AddVerifiedMap(501, "Piet Village");
         AddVerifiedMap(426, "Piet Dungeon Vestibule");

         // Mehadi
         AddVerifiedMap(3071, "Mehadi Entrance");
         AddVerifiedMap(3072, "Mehadi Hema");

         // Undine
         AddVerifiedMap(3008, "Undine Village Way");
         AddVerifiedMap(504, "Undine Village");

         // Suomi
         AddVerifiedMap(3016, "Suomi Village Way");
         AddVerifiedMap(503, "Suomi Village");

         // Astrid
         AddVerifiedMap(3060, "Astrid Entrance");

         // Undine Field
         AddVerifiedMap(5201, "Undine Field Entrance");

         // Mount Giragan
         AddVerifiedMap(2120, "Mount Giragan 1");

         // Base Camp (Grass Field)
         AddVerifiedMap(701, "Base Camp");

         // Loures Harbor
         AddVerifiedMap(6925, "Loures Harbor");

         // BNC
         AddVerifiedMap(6996, "Excavation Camp");

         // House Macabre
         AddVerifiedMap(2052, "Road to House Macabre");

         // Tagor
         AddVerifiedMap(662, "Tagor Village");

         // Oren
         AddVerifiedMap(6228, "Oren Island City");
         AddVerifiedMap(7200, "Oren Fair Entrance");

         // Ruc
         AddVerifiedMap(505, "Rucesion Village");
         AddVerifiedMap(3010, "Rucesion Village Way");
         AddVerifiedMap(340, "Dubhaim Castle");

         // Lynith
         AddVerifiedMap(6628, "Lynith Beach Way");
         AddVerifiedMap(6627, "South Lynith Beach");
         AddVerifiedMap(6625, "North Lynith Beach 1");
         AddVerifiedMap(6626, "North Lynith Beach 2");

         // Shinewood
         AddVerifiedMap(542, "Shinewood Forest Entranc");
         AddVerifiedMap(565, "Shinewood Forest 2");
         AddVerifiedMap(564, "Shinewood Forest 3");

         // Med Ship
         AddVerifiedMap(6742, "Passenger Ship 18");
         AddVerifiedMap(6731, "Passenger Ship 7");

         // Medenia
         AddVerifiedMap(10000, "Asilon Town");
         AddVerifiedMap(10055, "Noam Village");
      }

      public int Count { get { return maps.Count; } }

      public IEnumerable<MapSecurityInformation> Maps { get { return from m in maps.Values select m; } }

      public void AddMap(MapSecurityInformation map)
      {
         if (map == null)
            throw new ArgumentNullException("map");

         maps[map.MapNumber] = map;
      }

      void AddVerifiedMap(int mapNumber, string mapName)
      {
         var mapHash = ComputeMapHash(mapNumber, mapName);
         var mapInfo = new MapSecurityInformation(mapNumber, mapName, mapHash);

         AddMap(mapInfo);
      }

      public bool ContainsMap(int mapNumber)
      {
         return maps.ContainsKey(mapNumber);
      }

      public bool IsMapAllowed(string mapHash)
      {
         if (string.IsNullOrWhiteSpace(mapHash))
            return false;

         foreach (var map in maps.Values)
            if (string.Equals(map.MapHash, mapHash, StringComparison.OrdinalIgnoreCase))
               return true;

         return false;
      }

      public MapSecurityInformation GetMap(int mapNumber)
      {
         if (maps.ContainsKey(mapNumber))
            return maps[mapNumber];
         else
            return null;
      }

      public bool RemoveMap(int mapNumber)
      {
         MapSecurityInformation info;
         return maps.TryRemove(mapNumber, out info);
      }

      public void ClearMaps()
      {
         maps.Clear();
      }

      public string ComputeMapHash(int mapNumber, string mapName)
      {
         if (mapName == null)
            throw new ArgumentNullException("mapName");

         var saltedMapNumber = (mapNumber ^ (mapNumber % 2 == 0 ? MapNumberSaltEven : MapNumberSaltOdd));
         var rotatedMapName = Rotate13String(mapName);

         var mapString = string.Format("^{1}{0}{2}$", rotatedMapName, saltedMapNumber.ToString("X4"), (saltedMapNumber ^ 0xFFFF).ToString("X4"));

         if (hashCache.ContainsKey(mapString))
            return hashCache[mapString];

         using (var md5 = new MD5CryptoServiceProvider())
         {
            var asciiBytes = Encoding.ASCII.GetBytes(mapString);
            md5.ComputeHash(asciiBytes);

            var hashBuilder = new StringBuilder(asciiBytes.Length * 2);

            foreach (var hashByte in md5.Hash)
               hashBuilder.Append(hashByte.ToString("x2"));

            var hashString = hashBuilder.ToString();
            hashCache[mapString] = hashString;

            return hashString;
         }
      }

      static string Rotate13String(string text)
      {
         if (text == null)
            return string.Empty;

         var sb = new StringBuilder(text.Length);

         foreach (char c in text)
         {
            byte value = (byte)c;

            if (value >= 'a' && value <= 'z')
            {
               if (value > 'm')
                  value -= 13;
               else
                  value += 13;
            }
            else if (value >= 'A' && value <= 'Z')
            {
               if (value > 'M')
                  value -= 13;
               else
                  value += 13;
            }

            sb.Append((char)value);
         }

         return sb.ToString();
      }
   }
}
