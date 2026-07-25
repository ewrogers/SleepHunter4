using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Configuration
{
    public sealed class FileMacroConfigurationReader :
        IMacroConfigurationReader
    {
        public Task<MacroConfigurationLoadResult> LoadAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = Path.GetFullPath(filePath);
            return Task.Run(
                () => MacroConfigurationSerializer.Load(fullPath),
                cancellationToken);
        }
    }
}
