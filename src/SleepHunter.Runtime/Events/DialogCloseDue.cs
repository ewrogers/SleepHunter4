using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Events;

public sealed record DialogCloseDue(MacroTimestamp DueAt) : MacroEvent;
