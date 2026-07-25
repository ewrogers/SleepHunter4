using System.Text;

namespace SleepHunter.Interop.Snapshots;

internal static class Usda741Text
{
    private static readonly Encoding StrictAscii = Encoding.GetEncoding(
        Encoding.ASCII.CodePage,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);

    public static string ReadNullTerminatedAscii(ReadOnlySpan<byte> bytes)
    {
        var terminator = bytes.IndexOf((byte)0);
        if (terminator >= 0)
        {
            bytes = bytes[..terminator];
        }

        try
        {
            return StrictAscii.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException(
                "The client snapshot contains invalid ASCII text.",
                exception);
        }
    }
}
