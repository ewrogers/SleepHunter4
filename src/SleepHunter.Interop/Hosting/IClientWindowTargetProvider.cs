using SleepHunter.Interop.Input;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Hosting;

public interface IClientWindowTargetProvider
{
    ClientIdentity Client { get; }

    bool TryGetTarget(out ClientWindowTarget? target);
}
