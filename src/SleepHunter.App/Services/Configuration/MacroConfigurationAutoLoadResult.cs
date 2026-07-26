namespace SleepHunter.Services.Configuration
{
    public sealed record MacroConfigurationAutoLoadResult(
        MacroConfigurationApplyResult Applied,
        string SourcePath,
        bool MigratedLegacyFile);
}
