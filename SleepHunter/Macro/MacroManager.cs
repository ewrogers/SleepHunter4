using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

using SleepHunter.Models;

namespace SleepHunter.Macro
{
    internal sealed class MacroManager
    {
        private static readonly MacroManager instance = new MacroManager();

        public static MacroManager Instance => instance;

        private MacroManager() { }

        private readonly ConcurrentDictionary<int, PlayerMacroState> clientMacros = new ConcurrentDictionary<int, PlayerMacroState>();

        public int Count => clientMacros.Count;

        public IEnumerable<PlayerMacroState> Macros => clientMacros.Values;

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

        public void ImportMacroState(Player player, SavedMacroState state)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var macro = GetMacroState(player);
            if (macro == null)
                return;

            macro.Stop();

            var process = Process.GetCurrentProcess();

            if (player.HasHotkey)
            {
                HotkeyManager.Instance.UnregisterHotkey(process.MainWindowHandle, player.Hotkey);
                player.Hotkey = null;
            }

            player.Skillbook.ClearActiveSkills();
            macro.ClearSpellQueue();
            macro.ClearFlowerQueue();

            player.Update(PlayerFieldFlags.Spellbook);
            macro.UseLyliacVineyard = player.HasLyliacVineyard && state.UseLyliacVineyard;
            macro.FlowerAlternateCharacters = player.HasLyliacPlant && state.FlowerAlternateCharacters;

            player.Update(PlayerFieldFlags.Skillbook);
            foreach (var skill in state.Skills)
            {
                if (!player.Skillbook.ContainSkill(skill.SkillName))
                    continue;

                player.Skillbook.ToggleActive(skill.SkillName, true);
            }

            foreach (var spell in state.Spells)
            {
                if (!player.Spellbook.ContainSpell(spell.SpellName))
                    continue;

                var spellInfo = player.Spellbook.GetSpell(spell.SpellName);
                if (spellInfo == null)
                    continue;

                var queueItem = new SpellQueueItem(spellInfo, spell);
                macro.AddToSpellQueue(queueItem);
            }

            if (player.HasLyliacPlant)
                foreach (var flower in state.Flowers)
                {
                    if (flower.TargetMode == TargetCoordinateUnits.None)
                        continue;

                    var queueItem = new FlowerQueueItem(flower);
                    macro.AddToFlowerQueue(queueItem);
                }

            var windowHandle = Process.GetCurrentProcess().MainWindowHandle;

            if (state.HotkeyKey != Key.None && (state.HotkeyModifiers != ModifierKeys.None || Hotkey.IsFunctionKey(state.HotkeyKey)))
            {
                var hotkey = new Hotkey(state.HotkeyModifiers, state.HotkeyKey);

                if (HotkeyManager.Instance.RegisterHotkey(windowHandle, hotkey))
                    player.Hotkey = hotkey;
            }
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
