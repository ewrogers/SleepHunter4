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
        private readonly IMacroStateSerializer serializer;

        public RuntimeMacroConfigurationAdapter(
            IMacroStateSerializer serializer)
        {
            this.serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
        }

        public MacroConfigurationLoadResult Adapt(
            PlayerMacroState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            using var serialized = new StringWriter(
                CultureInfo.InvariantCulture);
            serializer.Serialize(state, serialized);
            using var reader = new StringReader(serialized.ToString());
            return MacroConfigurationSerializer.Load(reader);
        }
    }
}
