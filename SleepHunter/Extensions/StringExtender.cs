using System.Text;

namespace SleepHunter.Extensions
{
    internal static class StringExtender
    {
        public static string StripNumbers(this string text)
        {
            if (text == null)
                return null;

            var sb = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                if (char.IsDigit(c) || char.IsNumber(c))
                    continue;

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
