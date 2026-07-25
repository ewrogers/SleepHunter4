using System.Text;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Mappings;

public sealed class MappedMemoryReader
{
    private readonly ClientMemoryMap map;
    private readonly MemoryReadSession session;

    public MappedMemoryReader(
        ClientMemoryMap map,
        MemoryReadSession session)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(session);

        if (map.PointerWidth != session.Limits.PointerWidth)
        {
            throw new ArgumentException(
                "The memory map and read session pointer widths must match.",
                nameof(session));
        }

        this.map = map;
        this.session = session;
    }

    public ClientMemoryMap Map => map;

    public MemoryReadSession Session => session;

    public bool TryResolveAddress(
        string key,
        out MemoryAddress address,
        out MappedMemoryReadError? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var definition = map.Find(key);
        if (definition is null)
        {
            address = default;
            error = new MappedMemoryReadError(
                MappedMemoryReadFailure.VariableNotFound,
                key.Trim());
            return false;
        }

        return TryResolveAddress(definition, out address, out error);
    }

    public bool TryReadByte(
        string key,
        out byte value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.Byte,
            session.TryReadByte,
            out value,
            out error);

    public bool TryReadSByte(
        string key,
        out sbyte value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.SByte,
            session.TryReadSByte,
            out value,
            out error);

    public bool TryReadInt16(
        string key,
        out short value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.Signed16,
            session.TryReadInt16,
            out value,
            out error);

    public bool TryReadUInt16(
        string key,
        out ushort value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.Unsigned16,
            session.TryReadUInt16,
            out value,
            out error);

    public bool TryReadInt32(
        string key,
        out int value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.Signed32,
            session.TryReadInt32,
            out value,
            out error);

    public bool TryReadUInt32(
        string key,
        out uint value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.Unsigned32,
            session.TryReadUInt32,
            out value,
            out error);

    public bool TryReadInt64(
        string key,
        out long value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.Signed64,
            session.TryReadInt64,
            out value,
            out error);

    public bool TryReadUInt64(
        string key,
        out ulong value,
        out MappedMemoryReadError? error) =>
        TryRead(
            key,
            MemoryValueKind.Unsigned64,
            session.TryReadUInt64,
            out value,
            out error);

    public bool TryReadText(
        string key,
        Encoding encoding,
        out string? value,
        out MappedMemoryReadError? error,
        bool requireTerminator = false)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        if (!TryResolve(
                key,
                MemoryValueKind.Text,
                out var definition,
                out var address,
                out error))
        {
            value = null;
            return false;
        }

        if (!session.TryReadString(
                address,
                definition.MaximumLength,
                encoding,
                out value,
                out var memoryError,
                requireTerminator))
        {
            error = ReadFailed(definition.Key, memoryError);
            return false;
        }

        error = null;
        return true;
    }

    public bool TryReadBytes(
        string key,
        Span<byte> destination,
        out MappedMemoryReadError? error)
    {
        if (!TryResolve(
                key,
                MemoryValueKind.Binary,
                out var definition,
                out var address,
                out error))
        {
            return false;
        }

        if (!session.TryRead(address, destination, out var memoryError))
        {
            error = ReadFailed(definition.Key, memoryError);
            return false;
        }

        error = null;
        return true;
    }

    private bool TryRead<T>(
        string key,
        MemoryValueKind expectedKind,
        TypedReader<T> reader,
        out T value,
        out MappedMemoryReadError? error)
    {
        if (!TryResolve(
                key,
                expectedKind,
                out var definition,
                out var address,
                out error))
        {
            value = default!;
            return false;
        }

        if (!reader(address, out value, out var memoryError))
        {
            error = ReadFailed(definition.Key, memoryError);
            return false;
        }

        error = null;
        return true;
    }

    private bool TryResolve(
        string key,
        MemoryValueKind expectedKind,
        out MemoryVariableDefinition definition,
        out MemoryAddress address,
        out MappedMemoryReadError? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var found = map.Find(key);
        if (found is null)
        {
            definition = null!;
            address = default;
            error = new MappedMemoryReadError(
                MappedMemoryReadFailure.VariableNotFound,
                key.Trim(),
                expectedKind);
            return false;
        }

        definition = found;
        if (definition.ValueKind != expectedKind)
        {
            address = default;
            error = new MappedMemoryReadError(
                MappedMemoryReadFailure.ValueKindMismatch,
                definition.Key,
                expectedKind,
                definition.ValueKind);
            return false;
        }

        return TryResolveAddress(definition, out address, out error);
    }

    private bool TryResolveAddress(
        MemoryVariableDefinition definition,
        out MemoryAddress address,
        out MappedMemoryReadError? error)
    {
        if (definition.RequiresSearch)
        {
            address = default;
            error = new MappedMemoryReadError(
                MappedMemoryReadFailure.SearchResolutionRequired,
                definition.Key,
                ActualKind: definition.ValueKind);
            return false;
        }

        if (!PointerChainResolver.TryResolve(
                definition.Address,
                session,
                out address,
                out var memoryError))
        {
            error = new MappedMemoryReadError(
                MappedMemoryReadFailure.AddressResolutionFailed,
                definition.Key,
                ActualKind: definition.ValueKind,
                MemoryError: memoryError);
            return false;
        }

        error = null;
        return true;
    }

    private static MappedMemoryReadError ReadFailed(
        string key,
        MemoryReadError? error) =>
        new(
            MappedMemoryReadFailure.ValueReadFailed,
            key,
            MemoryError: error);

    private delegate bool TypedReader<T>(
        MemoryAddress address,
        out T value,
        out MemoryReadError? error);
}
