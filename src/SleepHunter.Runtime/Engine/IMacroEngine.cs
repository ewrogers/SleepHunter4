using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public interface IMacroEngine
{
    MacroDecision Decide(
        MacroState currentState,
        MacroEvent input,
        MacroTimestamp currentTime);
}
