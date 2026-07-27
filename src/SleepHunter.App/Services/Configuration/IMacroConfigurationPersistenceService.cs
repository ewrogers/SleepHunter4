using System.Threading;
using System.Threading.Tasks;
using SleepHunter.ViewModels.Editing;

namespace SleepHunter.Services.Configuration
{
    public interface IMacroConfigurationPersistenceService
    {
        Task<MacroConfigurationApplyResult> LoadAsync(
            ClientMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            ClientMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default);

        Task<MacroConfigurationAutoLoadResult> AutoLoadAsync(
            ClientMacroConfiguration configuration,
            CancellationToken cancellationToken = default);

        Task AutoSaveAsync(
            ClientMacroConfiguration configuration,
            CancellationToken cancellationToken = default);
    }
}
