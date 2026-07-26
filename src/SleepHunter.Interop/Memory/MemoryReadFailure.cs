namespace SleepHunter.Interop.Memory;

public enum MemoryReadFailure
{
    InvalidAddress,
    InvalidLength,
    BlockLimitExceeded,
    StringLimitExceeded,
    ByteBudgetExceeded,
    ReadBudgetExceeded,
    TransportFailure,
    PartialRead,
    PointerDepthExceeded,
    NullPointer,
    AddressOverflow,
    MissingTerminator,
    InvalidEncoding,
    UnsupportedValueKind
}
