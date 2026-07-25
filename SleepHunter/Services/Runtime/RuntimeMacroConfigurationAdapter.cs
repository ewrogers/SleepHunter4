using System;
using System.Globalization;
using System.IO;
using SleepHunter.Macro;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Services.Serialization;

namespace SleepHunter.Services.Runtime
{
    public sealed class RuntimeMacroConfigurationAdapter :
        IRuntimeMacroConfigurationAdapter
    {
        private readonly ILegacyMacroConfigurationSerializer serializer;

        public RuntimeMacroConfigurationAdapter(
            ILegacyMacroConfigurationSerializer serializer)
        {
            this.serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        public MacroConfigurationLoadResult Adapt(
            PlayerMacroConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            using var serialized = new StringWriter(
                CultureInfo.InvariantCulture);
            serializer.Serialize(configuration, serialized);
            using var reader = new StringReader(serialized.ToString());
            return MacroConfigurationSerializer.Load(reader);
        }
    }
}
