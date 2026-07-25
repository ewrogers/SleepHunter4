namespace SleepHunter.Runtime.Commands;

public abstract record MacroCommand;

public sealed record StartMacroCommand : MacroCommand;

public sealed record PauseMacroCommand : MacroCommand;

public sealed record ResumeMacroCommand : MacroCommand;

public sealed record StopMacroCommand : MacroCommand;
