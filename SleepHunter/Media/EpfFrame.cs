
namespace SleepHunter.Media
{
    internal sealed class EpfFrame
    {
        public int Index { get; }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public byte[] RawData { get; }

        public EpfFrame(int index, int x, int y, int width, int height, byte[] rawData)
        {
            Index = index;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            RawData = rawData;
        }

        public override string ToString() => $"Frame {Index}, Width={Width}, Height={Height}";
    }
}
