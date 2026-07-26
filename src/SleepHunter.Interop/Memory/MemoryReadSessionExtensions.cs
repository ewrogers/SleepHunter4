using System.Buffers.Binary;
using System.Text;

namespace SleepHunter.Interop.Memory;

public static class MemoryReadSessionExtensions
{
    public static bool TryReadByte(
        this MemoryReadSession session,
        MemoryAddress address,
        out byte value,
        out MemoryReadError? error)
    {
        ArgumentNullException.ThrowIfNull(session);

        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        if (!session.TryRead(address, buffer, out error))
        {
            value = default;
            return false;
        }

        value = buffer[0];
        return true;
    }

    public static bool TryReadSByte(
        this MemoryReadSession session,
        MemoryAddress address,
        out sbyte value,
        out MemoryReadError? error)
    {
        var success = session.TryReadByte(
            address,
            out var raw,
            out error);
        value = success
            ? unchecked((sbyte)raw)
            : default;
        return success;
    }

    public static bool TryReadInt16(
        this MemoryReadSession session,
        MemoryAddress address,
        out short value,
        out MemoryReadError? error)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        if (!session.TryRead(address, buffer, out error))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadInt16LittleEndian(buffer);
        return true;
    }

    public static bool TryReadUInt16(
        this MemoryReadSession session,
        MemoryAddress address,
        out ushort value,
        out MemoryReadError? error)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        if (!session.TryRead(address, buffer, out error))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        return true;
    }

    public static bool TryReadInt32(
        this MemoryReadSession session,
        MemoryAddress address,
        out int value,
        out MemoryReadError? error)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        if (!session.TryRead(address, buffer, out error))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        return true;
    }

    public static bool TryReadUInt32(
        this MemoryReadSession session,
        MemoryAddress address,
        out uint value,
        out MemoryReadError? error)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        if (!session.TryRead(address, buffer, out error))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        return true;
    }

    public static bool TryReadInt64(
        this MemoryReadSession session,
        MemoryAddress address,
        out long value,
        out MemoryReadError? error)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        if (!session.TryRead(address, buffer, out error))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadInt64LittleEndian(buffer);
        return true;
    }

    public static bool TryReadUInt64(
        this MemoryReadSession session,
        MemoryAddress address,
        out ulong value,
        out MemoryReadError? error)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        if (!session.TryRead(address, buffer, out error))
        {
            value = default;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        return true;
    }

    public static bool TryReadPointer(
        this MemoryReadSession session,
        MemoryAddress address,
        out MemoryAddress value,
        out MemoryReadError? error)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Limits.PointerWidth == PointerWidth.Bit32)
        {
            var success = session.TryReadUInt32(
                address,
                out var pointer,
                out error);
            value = new MemoryAddress(pointer);
            return success;
        }

        var result = session.TryReadUInt64(
            address,
            out var pointer64,
            out error);
        value = new MemoryAddress(pointer64);
        return result;
    }

    public static bool TryReadString(
        this MemoryReadSession session,
        MemoryAddress address,
        int maximumBytes,
        Encoding encoding,
        out string? value,
        out MemoryReadError? error,
        bool requireTerminator = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(encoding);

        if (maximumBytes <= 0 ||
            maximumBytes > session.Limits.MaximumStringBytes)
        {
            value = null;
            error = new MemoryReadError(
                maximumBytes <= 0
                    ? MemoryReadFailure.InvalidLength
                    : MemoryReadFailure.StringLimitExceeded,
                address,
                maximumBytes);
            return false;
        }

        var buffer = new byte[maximumBytes];
        if (!session.TryRead(address, buffer, out error))
        {
            value = null;
            return false;
        }

        var terminator = buffer.AsSpan().IndexOf((byte)0);
        if (terminator < 0 && requireTerminator)
        {
            value = null;
            error = new MemoryReadError(
                MemoryReadFailure.MissingTerminator,
                address,
                maximumBytes,
                maximumBytes);
            return false;
        }

        var length = terminator < 0
            ? buffer.Length
            : terminator;
        try
        {
            value = encoding.GetString(buffer, 0, length);
            error = null;
            return true;
        }
        catch (DecoderFallbackException)
        {
            value = null;
            error = new MemoryReadError(
                MemoryReadFailure.InvalidEncoding,
                address,
                maximumBytes,
                maximumBytes);
            return false;
        }
    }
}
