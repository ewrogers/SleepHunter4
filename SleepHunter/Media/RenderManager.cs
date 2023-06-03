using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SleepHunter.Media
{
    public sealed class RenderManager
    {
        private static readonly RenderManager instance = new();
        public static RenderManager Instance => instance;

        private RenderManager() { }

        public static IEnumerable<RenderedBitmap> Render(EpfImage image, ColorPalette palette)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            foreach (var frame in image.Frames)
                yield return Render(frame, palette);
        }

        public static RenderedBitmap Render(EpfFrame frame, ColorPalette palette)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            palette = palette.MakeGrayscale();
            var format = PixelFormats.Bgra32;

            var width = frame.Width;
            var height = frame.Height;
            var stride = (width * 4) + (width % 4);

            var bits = new byte[height * stride];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var fixedX = x;
                    var fixedY = y;

                    var threshold = 12;

                    if (fixedX < threshold)
                    {
                        if (width >= threshold)
                            fixedX = width - (threshold - fixedX);

                        if (fixedY > 0)
                            fixedY--;
                    }
                    else
                        fixedX -= threshold;

                    var pixel = frame.RawData[x + y * width];

                    if (pixel > 0)
                        SetPixel(bits, fixedX, fixedY, stride, palette[pixel]);
                    else
                        SetPixel(bits, fixedX, fixedY, stride, Colors.Transparent);
                }

            var bitmap = new RenderedBitmap(width, height, stride, format, bits);
            return bitmap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetPixel(byte[] bits, int x, int y, int stride, Color c)
        {
            var i = (x * 4) + (y * stride);

            bits[i] = c.B;
            bits[i + 1] = c.G;
            bits[i + 2] = c.R;
            bits[i + 3] = c.A;
        }
    }
}
