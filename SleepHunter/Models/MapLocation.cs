using System;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class MapLocation : UpdatableObject
    {
        private const string MapNumberKey = @"MapNumber";
        private const string MapNameKey = @"MapName";
        private const string MapXKey = @"MapX";
        private const string MapYKey = @"MapY";

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private int mapNumber;
        private int x;
        private int y;
        private string mapName;
        private string mapHash;

        public Player Owner { get; init; }

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
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }

        public bool IsSameMap(MapLocation other)
        {
            CheckIfDisposed();
            return MapNumber == other.MapNumber && string.Equals(MapName, other.MapName, StringComparison.Ordinal);
        }

        public bool IsWithinRange(MapLocation other, int maxX = 10, int maxY = 10)
        {
            CheckIfDisposed();

            if (!IsSameMap(other))
                return false;

            var deltaX = Math.Abs(X - other.X);
            var deltaY = Math.Abs(Y - other.Y);

            return deltaX <= maxX && deltaY <= maxY;
        }

        protected override void OnUpdate()
        {
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

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void ResetDefaults()
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
