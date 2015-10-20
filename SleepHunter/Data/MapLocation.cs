using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.IO;
using SleepHunter.IO.Process;

namespace SleepHunter.Data
{
   public sealed class MapLocation : NotifyObject
   {
      static readonly string MapNumberKey = @"MapNumber";
      static readonly string MapNameKey = @"MapName";
      static readonly string MapXKey = @"MapX";
      static readonly string MapYKey = @"MapY";

      Player owner;
      int mapNumber;
      int x;
      int y;
      string mapName;
      string mapHash;

      public Player Owner
      {
         get { return owner; }
         set { SetProperty(ref owner, value, "Owner"); }
      }

      public int MapNumber
      {
         get { return mapNumber; }
         set { SetProperty(ref mapNumber, value, "MapNumber"); }
      }

      public int X
      {
         get { return x; }
         set { SetProperty(ref x, value, "X"); }
      }

      public int Y
      {
         get { return y; }
         set { SetProperty(ref y, value, "Y"); }
      }

      public string MapName
      {
         get { return mapName; }
         set { SetProperty(ref mapName, value, "MapName"); }
      }

      public string MapHash
      {
         get { return mapHash; }
         set { SetProperty(ref mapHash, value, "MapHash"); }
      }

      public MapLocation()
         : this(null) { }

      public MapLocation(Player owner)
      {
         this.owner = owner;
      }

      public bool IsSameMap(MapLocation other)
      {
         return this.MapNumber == other.MapNumber && string.Equals(this.MapName, other.MapName, StringComparison.Ordinal);
      }

      public bool IsWithinRange(MapLocation other, int maxX = 10, int maxY = 10)
      {
         if (!this.IsSameMap(other))
            return false;

         var deltaX = Math.Abs(this.X - other.X);
         var deltaY = Math.Abs(this.Y - other.Y);

         return deltaX <= maxX && deltaY <= maxY;
      }

      public void Update()
      {
         if (owner == null)
            throw new InvalidOperationException("Player owner is null, cannot update.");

         Update(owner.Accessor);
      }

      public void Update(ProcessMemoryAccessor accessor)
      {
         if (accessor == null)
            throw new ArgumentNullException("accessor");

         var version = this.Owner.Version;

         if (version == null)
         {
            ResetDefaults();
            return;
         }

         var mapNumberVariable = version.GetVariable(MapNumberKey);         
         var mapXVariable = version.GetVariable(MapXKey);
         var mapYVariable = version.GetVariable(MapYKey);
         var mapNameVariable = version.GetVariable(MapNameKey);

         int mapNumber;
         int mapX, mapY;
         string mapName;
         
         using(var stream = accessor.GetStream())
         using (var reader = new BinaryReader(stream, Encoding.ASCII))
         {
            if (mapNumberVariable != null && mapNumberVariable.TryReadInt32(reader, out mapNumber))
               this.MapNumber = mapNumber;
            else
               this.MapNumber = 0;

            if (mapXVariable != null && mapXVariable.TryReadInt32(reader, out mapX))
               this.X = mapX;
            else
               this.X = 0;

            if (mapYVariable != null && mapYVariable.TryReadInt32(reader, out mapY))
               this.Y = mapY;
            else
               this.Y = 0;

            if (mapNameVariable != null && mapNameVariable.TryReadString(reader, out mapName))
               this.MapName = mapName;
            else
               this.MapName = null;
         }
      }

      public void ResetDefaults()
      {
         this.MapNumber = 0;
         this.X = 0;
         this.Y = 0;
         this.MapName = null;
         this.MapHash = null;
      }

      public override string ToString()
      {
         return string.Format("{0} [{1}] @ {2}, {3}", this.MapName ?? "Unknown Map",
            this.MapNumber.ToString(),
            this.X.ToString(),
            this.Y.ToString());
      } 
   }
}
