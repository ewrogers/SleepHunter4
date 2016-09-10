
namespace SleepHunter.Converters
{
  public static class ByteStringConverter
  {
    static readonly long KilobyteCount = 1024;
    static readonly long MegabyteCount = 1024L * 1024;
    static readonly long GigabyteCount = 1024L * 1024L * 1024L;
    static readonly long TerabyteCount = 1024L * 1024U * 1024L * 1024L;

    static readonly string ByteSuffix = "bytes";
    static readonly string KilobyteSuffix = "KB";
    static readonly string MegabyteSuffix = "MB";
    static readonly string GigabyteSuffix = "GB";
    static readonly string TerabyteSuffix = "TB";

    public static string GetEnglishString(double value, string format = null)
    {
      string suffix;
      long divisor = GetDenominator(value, out suffix);
      value /= divisor;

      if (format == null)
        return string.Format("{0} {1}", value.ToString(), suffix);
      else
        return string.Format("{0} {1}", value.ToString(format), suffix);
    }

    public static long GetDenominator(double value)
    {
      string suffix;
      return GetDenominator(value, out suffix);
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
