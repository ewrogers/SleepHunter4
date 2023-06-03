
namespace SleepHunter.Converters
{
    public static class ByteStringConverter
    {
        private const long KilobyteCount = 1024;
        private const long MegabyteCount = 1024L * 1024;
        private const long GigabyteCount = 1024L * 1024L * 1024L;
        private const long TerabyteCount = 1024L * 1024U * 1024L * 1024L;

        private const string ByteSuffix = "bytes";
        private const string KilobyteSuffix = "KB";
        private const string MegabyteSuffix = "MB";
        private const string GigabyteSuffix = "GB";
        private const string TerabyteSuffix = "TB";

        public static string GetEnglishString(double value, string format = null)
        {
            long divisor = GetDenominator(value, out var suffix);
            value /= divisor;

            if (format == null)
                return string.Format("{0} {1}", value.ToString(), suffix);
            else
                return string.Format("{0} {1}", value.ToString(format), suffix);
        }

        public static long GetDenominator(double value) => GetDenominator(value, out _);

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
