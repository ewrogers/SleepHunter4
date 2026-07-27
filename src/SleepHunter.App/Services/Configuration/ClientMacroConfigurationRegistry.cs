using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SleepHunter.Metadata;
using SleepHunter.Models;
using SleepHunter.ViewModels.Editing;

namespace SleepHunter.Services.Configuration
{
    public sealed class ClientMacroConfigurationRegistry
    {
        private readonly ConcurrentDictionary<
            int,
            ClientMacroConfiguration> configurations = new();
        private readonly SpellMetadataManager spellMetadata;

        public ClientMacroConfigurationRegistry()
            : this(new SpellMetadataManager())
        {
        }

        public ClientMacroConfigurationRegistry(
            SpellMetadataManager spellMetadata)
        {
            this.spellMetadata = spellMetadata ??
                throw new ArgumentNullException(
                    nameof(spellMetadata));
        }

        public IReadOnlyCollection<ClientMacroConfiguration>
            Configurations =>
            configurations.Values.ToArray();

        public ClientMacroConfiguration GetOrCreate(
            ClientSession session)
        {
            ArgumentNullException.ThrowIfNull(session);

            var processId = session.Process.ProcessId;
            var configuration = configurations.GetOrAdd(
                processId,
                _ => new ClientMacroConfiguration(
                    session,
                    spellMetadata));
            if (!ReferenceEquals(configuration.Client, session))
            {
                throw new InvalidOperationException(
                    $"Process {processId} changed session ownership without removing its macro configuration.");
            }

            return configuration;
        }

        public bool Remove(int processId) =>
            configurations.TryRemove(processId, out _);

        public void Clear() => configurations.Clear();
    }
}
