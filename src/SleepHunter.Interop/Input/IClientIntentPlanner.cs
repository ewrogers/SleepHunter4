using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Input;

public interface IClientIntentPlanner
{
    ClientIntentPlanResult Plan(
        ClientActionIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot);
}
