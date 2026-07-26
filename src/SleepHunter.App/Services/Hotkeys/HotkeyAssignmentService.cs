using System;
using System.Collections.Generic;
using System.Linq;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Services.Logging;

namespace SleepHunter.Services.Hotkeys
{
    public sealed class HotkeyAssignmentService
    {
        private readonly IHotkeyRegistrationService hotkeys;
        private readonly ILogger logger;

        public HotkeyAssignmentService(
            IHotkeyRegistrationService hotkeys,
            ILogger logger)
        {
            this.hotkeys = hotkeys ??
                throw new ArgumentNullException(nameof(hotkeys));
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        public HotkeyAssignmentResult Assign(
            Player player,
            Hotkey requested,
            IEnumerable<Player> players)
        {
            ArgumentNullException.ThrowIfNull(player);
            ArgumentNullException.ThrowIfNull(requested);
            ArgumentNullException.ThrowIfNull(players);

            var allPlayers = players.ToArray();
            if (allPlayers.Any(candidate => candidate is null))
            {
                throw new ArgumentException(
                    "The hotkey player set cannot contain null values.",
                    nameof(players));
            }

            var previous = player.Hotkey;
            var registered = hotkeys.Find(
                requested.Key,
                requested.Modifiers);
            if (ReferenceEquals(previous, registered))
            {
                return new HotkeyAssignmentResult(
                    HotkeyAssignmentStatus.Unchanged);
            }

            var registeredPrevious = previous is null
                ? null
                : hotkeys.Find(
                    previous.Key,
                    previous.Modifiers);
            var previousIsRegistered =
                ReferenceEquals(previous, registeredPrevious);

            if (registered is not null &&
                !hotkeys.Unregister(registered))
            {
                logger.LogError(
                    $"Unable to release hotkey {requested} before assigning it to {player.Name}");
                return new HotkeyAssignmentResult(
                    HotkeyAssignmentStatus.RegistrationFailed);
            }

            if (!hotkeys.Register(requested))
            {
                logger.LogError(
                    $"Unable to set hotkey {requested} for character: {player.Name}");
                Restore(registered);

                return new HotkeyAssignmentResult(
                    HotkeyAssignmentStatus.RegistrationFailed);
            }

            if (previousIsRegistered &&
                !SameGesture(previous, requested) &&
                !hotkeys.Unregister(previous))
            {
                logger.LogError(
                    $"Unable to release previous hotkey {previous} for character: {player.Name}");
                RollBack(requested, registered);
                return new HotkeyAssignmentResult(
                    HotkeyAssignmentStatus.RegistrationFailed);
            }

            foreach (var candidate in allPlayers)
            {
                if (!ReferenceEquals(candidate, player) &&
                    SameGesture(candidate.Hotkey, requested))
                {
                    candidate.Hotkey = null;
                }
            }

            player.Hotkey = requested;
            logger.LogInfo(
                $"Set hotkey {requested} for character: {player.Name}");
            return new HotkeyAssignmentResult(
                HotkeyAssignmentStatus.Assigned);
        }

        public HotkeyAssignmentResult Clear(Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            if (player.Hotkey is not { } assigned)
            {
                return new HotkeyAssignmentResult(
                    HotkeyAssignmentStatus.Unchanged);
            }

            var registered = hotkeys.Find(
                assigned.Key,
                assigned.Modifiers);
            if (ReferenceEquals(assigned, registered) &&
                !hotkeys.Unregister(assigned))
            {
                logger.LogError(
                    $"Unable to clear hotkey {assigned} for character: {player.Name}");
                return new HotkeyAssignmentResult(
                    HotkeyAssignmentStatus.RegistrationFailed);
            }

            logger.LogInfo(
                $"Clearing hotkey for character: {player.Name}");
            player.Hotkey = null;
            return new HotkeyAssignmentResult(
                HotkeyAssignmentStatus.Cleared);
        }

        private void RollBack(
            Hotkey requested,
            Hotkey displaced)
        {
            if (!hotkeys.Unregister(requested))
            {
                logger.LogError(
                    $"Unable to roll back hotkey {requested} after assignment failed");
            }

            Restore(displaced);
        }

        private void Restore(Hotkey hotkey)
        {
            if (hotkey is not null &&
                !hotkeys.Register(hotkey))
            {
                logger.LogError(
                    $"Unable to restore hotkey {hotkey} after assignment failed");
            }
        }

        private static bool SameGesture(
            Hotkey left,
            Hotkey right) =>
            left is not null &&
            right is not null &&
            left.Key == right.Key &&
            left.Modifiers == right.Modifiers;
    }
}
