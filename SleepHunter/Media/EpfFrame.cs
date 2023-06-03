
namespace SleepHunter.Media
{
    public sealed class EpfFrame
    {
        public int Index { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public byte[] RawData { get; set; }

        public EpfFrame() { }

        public EpfFrame(int index, int x, int y, int width, int height, byte[] rawData)
        {
            Index = index;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            RawData = rawData;
        }
    }
}
