using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text;

namespace SleepHunter.Media
{
   public sealed class RenderedBitmap
   {
      int width;
      int height;
      int stride;
      PixelFormat format;
      byte[] bits;

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
