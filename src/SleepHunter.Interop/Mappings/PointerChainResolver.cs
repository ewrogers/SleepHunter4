using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Mappings;

public static class PointerChainResolver
{
    public static bool TryResolve(
        PointerChain chain,
        MemoryReadSession session,
        out MemoryAddress address,
        out MemoryReadError? error)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(session);

        address = chain.BaseAddress;
        if (!session.Contains(address))
        {
            error = new MemoryReadError(
                MemoryReadFailure.InvalidAddress,
                address,
                RequestedBytes: 1);
            return false;
        }

        if (chain.Offsets.Length > session.Limits.MaximumPointerDepth)
        {
            error = new MemoryReadError(
                MemoryReadFailure.PointerDepthExceeded,
                address,
                RequestedBytes: 0);
            return false;
        }

        foreach (var offset in chain.Offsets)
        {
            var pointerAddress = address;
            if (!session.TryReadPointer(
                    pointerAddress,
                    out var pointer,
                    out error))
            {
                address = default;
                return false;
            }

            if (pointer.IsNull)
            {
                address = default;
                error = new MemoryReadError(
                    MemoryReadFailure.NullPointer,
                    pointerAddress,
                    (int)session.Limits.PointerWidth);
                return false;
            }

            if (!pointer.TryOffset(offset.Value, out address))
            {
                error = new MemoryReadError(
                    MemoryReadFailure.AddressOverflow,
                    pointer,
                    RequestedBytes: 1);
                return false;
            }

            if (!session.Contains(address))
            {
                error = new MemoryReadError(
                    MemoryReadFailure.InvalidAddress,
                    address,
                    RequestedBytes: 1);
                return false;
            }
        }

        error = null;
        return true;
    }
}
