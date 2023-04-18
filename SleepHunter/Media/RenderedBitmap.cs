using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SleepHunter.Media
{
    internal sealed class RenderedBitmap
    {
        private readonly int width;
        private readonly int height;
        private readonly int stride;
        private readonly PixelFormat format;
        private readonly byte[] bits;

        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public int Stride { get { return stride; } }
        public PixelFormat Format { get { return format; } }
        public byte[] Bits { get { return bits; } }

        public RenderedBitmap(int width, int height, int stride, PixelFormat format, byte[] bits)
        {
            this.width = width;
            this.height = height;
            this.stride = stride;
            this.format = format;
            this.bits = bits;
        }

        public BitmapSource CreateBitmapSource(double dpiX = 96.0, double dpiY = 96.0)
        {
            return BitmapSource.Create(width, height, dpiX, dpiY, format, null, bits, stride);
        }
    }
}
