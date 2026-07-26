using System.IO;
using System.Text;

namespace SleepHunter.Extensions
{
    public static class BinaryReaderExtensions
    {
        public static string ReadFixedString(this BinaryReader reader, int length)
        {
            var byteBuffer = new byte[length];
            var asciiBuffer = new char[length];
            var stringBuffer = new StringBuilder(length);

            reader.Read(byteBuffer, 0, length);
            int charCount = Encoding.ASCII.GetChars(byteBuffer, 0, length, asciiBuffer, 0);

            for (int i = 0; i < charCount; i++)
            {
                var c = asciiBuffer[i];

                if (c == '\0')
                    break;

                stringBuffer.Append(c);
            }

            return stringBuffer.ToString();
        }

    }
}
