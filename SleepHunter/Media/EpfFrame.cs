
namespace SleepHunter.Media
{
    public sealed class EpfFrame
    {
        int index;
        int x;
        int y;
        int width;
        int height;
        byte[] rawData;

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public byte[] RawData
        {
            get { return rawData; }
            set { rawData = value; }
        }

        public EpfFrame() { }

        public EpfFrame(int index, int x, int y, int width, int height, byte[] rawData)
        {
            this.index = index;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.rawData = rawData;
        }
    }
}
