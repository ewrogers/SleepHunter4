using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Persistence.Configuration;

namespace SleepHunter.Services.Configuration
{
    public interface IMacroConfigurationWriter
    {
        Task SaveAsync(
            MacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default);
    }
}
