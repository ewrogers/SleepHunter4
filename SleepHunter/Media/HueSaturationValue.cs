using System;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SleepHunter.Media
{
    [Serializable]
    public readonly struct HueSaturationValue
    {
        [XmlIgnore]
        public Color Color { get; }
        public double Hue { get; }
        public double Saturation { get; }
        public double Value { get; }

        public HueSaturationValue(Color color)
           : this()
        {
            Color = color;
            CalculateHSV(color.R, color.G, color.B, out var hue, out var sat, out var value);

            Hue = hue;
            Saturation = sat;
            Value = value;
        }

        private static void CalculateHSV(byte red, byte green, byte blue, out double hue, out double saturation, out double value)
        {
            hue = 0;
            var rgbMax = (double)Math.Max(Math.Max(red, green), blue);

            value = rgbMax;

            if (value == 0)
            {
                hue = 0;
                saturation = 0;
                return;
            }

            var r = red / rgbMax;
            var g = green / rgbMax;
            var b = blue / rgbMax;

            rgbMax = Max3(r, g, b);
            var rgbMin = Min3(r, g, b);

            saturation = rgbMax - rgbMin;

            r = (r - rgbMin) / (rgbMax - rgbMin);
            g = (g - rgbMin) / (rgbMax - rgbMin);
            b = (b - rgbMin) / (rgbMax - rgbMin);

            rgbMax = Max3(r, g, b);

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

        private static double Max3(double a, double b, double c)
        {
            return (b >= c) ?
               (a >= b) ? a : b :
               (a >= c) ? a : c;
        }

        private static double Min3(double a, double b, double c)
        {
            return (b <= c) ?
               (a <= b) ? a : b :
               (a <= c) ? a : c;
        }
    }
}
