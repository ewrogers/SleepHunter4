using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Hosting;

public interface IClientRuntimeFactory
{
    IClientRuntimeHost Attach(
        Stream mappingStream,
        ClientIdentity client,
        int processId,
        nint windowHandle,
        SnapshotCaptureSchedule snapshotSchedule,
        MacroClock clock,
        AbilitySnapshotCatalog? abilityCatalog = null);
}
