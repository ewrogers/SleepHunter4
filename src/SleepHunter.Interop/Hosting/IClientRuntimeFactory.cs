using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Hosting;

public interface IClientRuntimeFactory
{
    IClientRuntimeHost Attach(
        Stream mappingStream,
        ClientIdentity client,
        int processId,
        nint windowHandle,
        SnapshotCaptureSchedule snapshotSchedule,
        TimeProvider timeProvider,
        AbilitySnapshotCatalog? abilityCatalog = null);
}
