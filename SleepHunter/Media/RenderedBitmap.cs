using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SleepHunter.Media
{
    public sealed class RenderedBitmap
    {
        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public PixelFormat Format { get; }
        public byte[] Bits { get; }

        public RenderedBitmap(int width, int height, int stride, PixelFormat format, byte[] bits)
        {
            Width = width;
            Height = height;
            Stride = stride;
            Format = format;
            Bits = bits ?? throw new ArgumentNullException(nameof(bits));
        }

        public BitmapSource CreateBitmapSource(double dpiX = 96.0, double dpiY = 96.0)
            => BitmapSource.Create(Width, Height, dpiX, dpiY, Format, null, Bits, Stride);
    }
}
