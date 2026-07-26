using SleepHunter.Common;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Models
{
    public sealed class MapLocation : ObservableObject
    {
        private int x;
        private int y;
        private string mapName;

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

        internal void Apply(MapLocationSnapshot snapshot)
        {
            MapName = snapshot?.MapName;
            X = snapshot?.X ?? 0;
            Y = snapshot?.Y ?? 0;
        }

        internal void Reset() => Apply(null);

    }
}
