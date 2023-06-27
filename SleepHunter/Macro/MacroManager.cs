using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using SleepHunter.Models;

namespace SleepHunter.Macro
{
    public sealed class MacroManager
    {
        private static readonly MacroManager instance = new();

        public static MacroManager Instance => instance;

        private MacroManager() { }

        private readonly ConcurrentDictionary<int, PlayerMacroState> clientMacros = new();

        public int Count => clientMacros.Count;

        public IEnumerable<PlayerMacroState> Macros => from m in clientMacros.Values select m;

        public PlayerMacroState GetMacroState(Player player)
        {
            if (clientMacros.ContainsKey(player.Process.ProcessId))
                return clientMacros[player.Process.ProcessId];

            var state = new PlayerMacroState(player);
            clientMacros[player.Process.ProcessId] = state;

            return state;
        }

        public bool RemoveMacroState(int processId)
        {
            var wasRemoved = clientMacros.TryRemove(processId, out var removedClient);

            if (wasRemoved && removedClient != null)
                removedClient.Stop();

            return wasRemoved;
        }

        public void ClearMacros()
        {
            foreach (var processId in clientMacros.Keys.ToArray())
                RemoveMacroState(processId);

            clientMacros.Clear();
        }

        public void StartAll()
        {
            foreach (var macro in clientMacros.Values)
                macro.Start();
        }

        public void ResumeAll()
        {
            foreach (var macro in clientMacros.Values)
                if (macro.Status == MacroStatus.Paused)
                    macro.Start();
        }

        public void PauseAll()
        {
            foreach (var macro in clientMacros.Values)
                if (macro.Status == MacroStatus.Running)
                    macro.Pause();
        }

        public void StopAll()
        {
            foreach (var macro in clientMacros.Values)
                macro.Stop();
        }
    }
}
