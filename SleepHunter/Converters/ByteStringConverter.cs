
namespace SleepHunter.Converters
{
    internal static class ByteStringConverter
    {
        private static readonly long KilobyteCount = 1024;
        private static readonly long MegabyteCount = 1024L * 1024;
        private static readonly long GigabyteCount = 1024L * 1024L * 1024L;
        private static readonly long TerabyteCount = 1024L * 1024U * 1024L * 1024L;

        private static readonly string ByteSuffix = "bytes";
        private static readonly string KilobyteSuffix = "KB";
        private static readonly string MegabyteSuffix = "MB";
        private static readonly string GigabyteSuffix = "GB";
        private static readonly string TerabyteSuffix = "TB";

        public static string GetEnglishString(double value, string format = null)
        {
            long divisor = GetDenominator(value, out var suffix);
            value /= divisor;

            if (format == null)
                return string.Format("{0} {1}", value.ToString(), suffix);
            else
                return string.Format("{0} {1}", value.ToString(format), suffix);
        }

        public static long GetDenominator(double value)
        {
            return GetDenominator(value, out var _);
        }

        public static long GetDenominator(double value, out string denominatorString)
        {
            denominatorString = ByteSuffix;

            if (value >= TerabyteCount)
            {
                denominatorString = TerabyteSuffix;
                return TerabyteCount;
            }

            if (value >= GigabyteCount)
            {
                denominatorString = GigabyteSuffix;
                return GigabyteCount;
            }

            if (value >= MegabyteCount)
            {
                denominatorString = MegabyteSuffix;
                return MegabyteCount;
            }

            if (value >= KilobyteCount)
            {
                denominatorString = KilobyteSuffix;
                return KilobyteCount;
            }

            return 1;
        }
    }
}
