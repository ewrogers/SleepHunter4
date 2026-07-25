using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;

namespace SleepHunter.Services.Configuration
{
    public sealed class FileMacroConfigurationWriter :
        IMacroConfigurationWriter
    {
        public Task SaveAsync(
            MacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = Path.GetFullPath(filePath);
            return Task.Run(
                () => MacroConfigurationSerializer.Save(
                    configuration,
                    fullPath),
                cancellationToken);
        }
    }
}
