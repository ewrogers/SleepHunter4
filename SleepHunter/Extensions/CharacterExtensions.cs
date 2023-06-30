
namespace SleepHunter.Extensions
{
    public static class CharacterExtensions
    {
        public static bool IsValidHexDigit(this char c, bool allowControl = true)
        {
            var isControl = char.IsControl(c);
            var isDigit = char.IsDigit(c);
            var isHexDigit = (c == 'a' || c == 'A') ||
               (c == 'b' || c == 'B') ||
               (c == 'c' || c == 'C') ||
               (c == 'd' || c == 'D') ||
               (c == 'e' || c == 'E') ||
               (c == 'f' || c == 'F');

            var isHex = isDigit || isHexDigit;

            if (allowControl)
                isHex |= isControl;

            return isHex;
        }

        public static bool IsValidDecimalCharacter(this char c, bool allowControl = true)
        {
            var isControl = char.IsControl(c);
            var isDigit = char.IsDigit(c);

            var isDec = isDigit;

            if (allowControl)
                isDec |= isControl;

            return isDec;
        }
    }
}
