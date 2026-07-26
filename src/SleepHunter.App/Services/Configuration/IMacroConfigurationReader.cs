using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Configuration
{
    public interface IMacroConfigurationReader
    {
        Task<MacroConfigurationLoadResult> LoadAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }
}
