namespace SleepHunter.Interop.Mappings;

public enum MappedMemoryReadFailure
{
    VariableNotFound,
    ValueKindMismatch,
    SearchResolutionRequired,
    AddressResolutionFailed,
    ValueReadFailed
}
