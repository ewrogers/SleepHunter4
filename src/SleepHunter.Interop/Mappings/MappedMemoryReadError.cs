using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Mappings;

public sealed record MappedMemoryReadError(
    MappedMemoryReadFailure Failure,
    string VariableKey,
    MemoryValueKind? ExpectedKind = null,
    MemoryValueKind? ActualKind = null,
    MemoryReadError? MemoryError = null);
