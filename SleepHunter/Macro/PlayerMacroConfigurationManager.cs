using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SleepHunter.Models;

namespace SleepHunter.Macro
{
    public sealed class PlayerMacroConfigurationManager
    {
        private readonly ConcurrentDictionary<
            int,
            PlayerMacroConfiguration> configurations = new();

        public IReadOnlyCollection<PlayerMacroConfiguration>
            Configurations =>
            configurations.Values.ToArray();

        public PlayerMacroConfiguration GetOrCreate(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            var processId = player.Process.ProcessId;
            var configuration = configurations.GetOrAdd(
                processId,
                _ => new PlayerMacroConfiguration(player));
            if (!ReferenceEquals(configuration.Client, player))
            {
                throw new InvalidOperationException(
                    $"Process {processId} changed player ownership without removing its macro configuration.");
            }

            return configuration;
        }

        public bool Remove(int processId) =>
            configurations.TryRemove(processId, out _);

        public void Clear() => configurations.Clear();
    }
}
