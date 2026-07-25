using System.Collections.Immutable;
using SleepHunter.Persistence.Configuration;

namespace SleepHunter.Persistence.Serialization;

public sealed record MacroConfigurationLoadResult(
    MacroConfiguration Configuration,
    MacroConfigurationFormat Format,
    string SourceVersion,
    ImmutableArray<MacroConfigurationWarning> Warnings);
