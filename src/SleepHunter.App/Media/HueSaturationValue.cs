using System;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SleepHunter.Media
{
    [Serializable]
    public struct HueSaturationValue
    {
        [NonSerialized]
        Color color;

        private double hue;
        private double saturation;
        private double value;

        public Color Color => color;
        public double Hue => hue;
        public double Saturation => saturation;
        public double Value => value;

        public HueSaturationValue(Color color)
           : this()
        {
            this.color = color;
            CalculateHSV();
        }

        private void CalculateHSV()
        {
            var rgbMax = (double)Math.Max(Math.Max(color.R, color.G), color.B);
            var rgbMin = (double)Math.Min(Math.Min(color.R, color.G), color.B);

            value = rgbMax;

            if (value == 0)
            {
                hue = 0;
                saturation = 0;
                return;
            }

            var r = color.R / rgbMax;
            var g = color.G / rgbMax;
            var b = color.B / rgbMax;

            rgbMax = Max3(r, g, b);
            rgbMin = Min3(r, g, b);

            saturation = rgbMax - rgbMin;

            r = (r - rgbMin) / (rgbMax - rgbMin);
            g = (g - rgbMin) / (rgbMax - rgbMin);
            b = (b - rgbMin) / (rgbMax - rgbMin);

            rgbMax = Max3(r, g, b);
            rgbMin = Min3(r, g, b);

            if (rgbMax == r)
            {
                hue = 60 * (g - b);

                while (hue < 0)
                    hue += 360;
            }
            else if (rgbMax == g)
            {
                hue = 120 + 60 * (b - r);
            }
            else if (rgbMax == b)
            {
                hue = 240 + 60 * (r - g);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Max3(double a, double b, double c)
        {
            return (b >= c) ?
               (a >= b) ? a : b :
               (a >= c) ? a : c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Min3(double a, double b, double c)
        {
            return (b <= c) ?
               (a <= b) ? a : b :
               (a <= c) ? a : c;
        }
    }
}
