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
            => Render(frame, palette, makeGrayscale: true, useLuminanceAlpha: false);

        public static RenderedBitmap RenderItem(EpfFrame frame, ColorPalette palette, bool useLuminanceAlpha = false)
            => Render(frame, palette, makeGrayscale: false, useLuminanceAlpha);

        private static RenderedBitmap Render(EpfFrame frame, ColorPalette palette, bool makeGrayscale,
            bool useLuminanceAlpha)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            if (makeGrayscale)
                palette = palette.MakeGrayscale();

            var format = PixelFormats.Bgra32;

            var width = frame.Width;
            var height = frame.Height;
            var stride = width * 4;

            var bits = new byte[height * stride];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var pixel = frame.RawData[x + y * width];

                    if (pixel > 0)
                    {
                        var color = palette[pixel];
                        if (useLuminanceAlpha)
                            color = Color.FromArgb(GetLuminanceAlpha(color), color.R, color.G, color.B);

                        SetPixel(bits, x, y, stride, color);
                    }
                    else
                        SetPixel(bits, x, y, stride, Colors.Transparent);
                }

            var bitmap = new RenderedBitmap(width, height, stride, format, bits);
            return bitmap;
        }

        private static byte GetLuminanceAlpha(Color color)
        {
            const double gamma = 2.0;
            var linearRed = Math.Pow(color.R / 255.0, gamma);
            var linearGreen = Math.Pow(color.G / 255.0, gamma);
            var linearBlue = Math.Pow(color.B / 255.0, gamma);
            var linearLuminance = 0.299 * linearRed + 0.587 * linearGreen + 0.114 * linearBlue;
            var luminance = Math.Pow(linearLuminance, 1.0 / gamma) * 255.0;
            return (byte)Math.Clamp(Math.Round(luminance), byte.MinValue, byte.MaxValue);
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
