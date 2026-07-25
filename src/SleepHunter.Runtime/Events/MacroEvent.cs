using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Events;

public abstract record MacroEvent;

public sealed record MacroCommandReceived(MacroCommand Command) : MacroEvent;

public sealed record ClientSnapshotObserved(ClientSnapshot Snapshot) : MacroEvent;
