using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Macro;

namespace SleepHunter.Services.Configuration
{
    public interface IMacroConfigurationPersistenceService
    {
        Task<MacroConfigurationApplyResult> LoadAsync(
            PlayerMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            PlayerMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default);

        Task<MacroConfigurationAutoLoadResult> AutoLoadAsync(
            PlayerMacroConfiguration configuration,
            CancellationToken cancellationToken = default);

        Task AutoSaveAsync(
            PlayerMacroConfiguration configuration,
            CancellationToken cancellationToken = default);
    }
}
