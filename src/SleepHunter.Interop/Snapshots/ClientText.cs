using System.Text;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientText
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

    public static string ReadNullTerminatedAsciiIgnoringNonAscii(
        ReadOnlySpan<byte> bytes)
    {
        var terminator = bytes.IndexOf((byte)0);
        if (terminator >= 0)
        {
            bytes = bytes[..terminator];
        }

        Span<byte> asciiBytes = bytes.Length <= 256
            ? stackalloc byte[bytes.Length]
            : new byte[bytes.Length];
        var asciiLength = 0;
        foreach (var value in bytes)
        {
            if (value <= 0x7F)
            {
                asciiBytes[asciiLength++] = value;
            }
        }

        return Encoding.ASCII.GetString(asciiBytes[..asciiLength]);
    }
}
