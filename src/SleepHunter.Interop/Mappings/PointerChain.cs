using System.Collections.Immutable;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Mappings;

public sealed record PointerChain
{
    public PointerChain(
        MemoryAddress baseAddress,
        ImmutableArray<PointerOffset> offsets = default)
    {
        BaseAddress = baseAddress;
        Offsets = offsets.IsDefault
            ? ImmutableArray<PointerOffset>.Empty
            : offsets;
    }

    public MemoryAddress BaseAddress { get; }

    public ImmutableArray<PointerOffset> Offsets { get; }

    public bool IsStatic => Offsets.IsEmpty;
}
