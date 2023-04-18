using System;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class MapLocation : ObservableObject
    {
        private static readonly string MapNumberKey = @"MapNumber";
        private static readonly string MapNameKey = @"MapName";
        private static readonly string MapXKey = @"MapX";
        private static readonly string MapYKey = @"MapY";

        private Player owner;
        private int mapNumber;
        private int x;
        private int y;
        private string mapName;
        private string mapHash;

        public Player Owner
        {
            get => owner;
            set => SetProperty(ref owner, value);
        }

        public int MapNumber
        {
            get => mapNumber;
            set => SetProperty(ref mapNumber, value);
        }

        public int X
        {
            get => x;
            set => SetProperty(ref x, value);
        }

        public int Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }

        public string MapName
        {
            get => mapName;
            set => SetProperty(ref mapName, value);
        }

        public string MapHash
        {
            get => mapHash;
            set => SetProperty(ref mapHash, value);
        }

        public MapLocation(Player owner)
            => this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

        public bool IsSameMap(MapLocation other) => 
            MapNumber == other.MapNumber && string.Equals(MapName, other.MapName, StringComparison.Ordinal);

        public bool IsWithinRange(MapLocation other, int maxX = 10, int maxY = 10)
        {
            if (!IsSameMap(other))
                return false;

            var deltaX = Math.Abs(X - other.X);
            var deltaY = Math.Abs(Y - other.Y);

            return deltaX <= maxX && deltaY <= maxY;
        }

        public void Update(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            var mapNumberVariable = version.GetVariable(MapNumberKey);
            var mapXVariable = version.GetVariable(MapXKey);
            var mapYVariable = version.GetVariable(MapYKey);
            var mapNameVariable = version.GetVariable(MapNameKey);

            Stream stream = null;
            try
            {
                stream = accessor.GetStream();
                using (var reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    stream = null;

                    if (mapNumberVariable != null && mapNumberVariable.TryReadInt32(reader, out var mapNumber))
                        MapNumber = mapNumber;
                    else
                        MapNumber = 0;

                    if (mapXVariable != null && mapXVariable.TryReadInt32(reader, out var mapX))
                        X = mapX;
                    else
                        X = 0;

                    if (mapYVariable != null && mapYVariable.TryReadInt32(reader, out var mapY))
                        Y = mapY;
                    else
                        Y = 0;

                    if (mapNameVariable != null && mapNameVariable.TryReadString(reader, out var mapName))
                        MapName = mapName;
                    else
                        MapName = null;
                }
            }
            finally { stream?.Dispose(); }
        }

        public void ResetDefaults()
        {
            MapNumber = 0;
            X = 0;
            Y = 0;
            MapName = null;
            MapHash = null;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] @ {2}, {3}", MapName ?? "Unknown Map",
               MapNumber.ToString(),
               X.ToString(),
               Y.ToString());
        }
    }
}
