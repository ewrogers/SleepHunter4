using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

using SleepHunter.IO.Process;
using SleepHunter.Settings;

namespace SleepHunter.Models
{
    internal sealed class PlayerManager : INotifyPropertyChanged
    {
        private static readonly PlayerManager instance = new PlayerManager();

        public static PlayerManager Instance
        {
            get { return instance; }
        }

        private PlayerManager() { }

        private readonly ConcurrentDictionary<int, Player> players = new ConcurrentDictionary<int, Player>();
        private PlayerSortOrder sortOrder = PlayerSortOrder.LoginTime;

        public event PlayerEventHandler PlayerAdded;
        public event PropertyChangedEventHandler PlayerPropertyChanged;
        public event PlayerEventHandler PlayerUpdated;
        public event PlayerEventHandler PlayerRemoved;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count { get { return players.Count; } }

        public PlayerSortOrder SortOrder
        {
            get => sortOrder;
            set => sortOrder = value;
        }

        public IEnumerable<Player> AllClients
        {
            get { return from p in players.Values orderby p.IsLoggedIn descending, p.Name, p.Process.ProcessId select p; }
        }

        public IEnumerable<Player> LoggedInPlayers
        {
            get { return from p in players.Values orderby p.Name where p.IsLoggedIn select p; }
        }

        public IEnumerable<Player> SortedPlayers
        {
            get
            {
                if (sortOrder == PlayerSortOrder.LoginTime)
                    return from p in players.Values orderby p.LoginTimestamp?.Ticks ?? p.Process.ProcessId where p.IsLoggedIn select p;
                else if (sortOrder == PlayerSortOrder.Alphabetical)
                    return from p in players.Values orderby p.Name where p.IsLoggedIn select p;
                else if (sortOrder == PlayerSortOrder.HighestHealth)
                    return from p in players.Values orderby p.Stats.MaximumHealth descending where p.IsLoggedIn select p;
                else if (sortOrder == PlayerSortOrder.HighestMana)
                    return from p in players.Values orderby p.Stats.MaximumMana descending where p.IsLoggedIn select p;
                else if (sortOrder == PlayerSortOrder.HighestCombined)
                    return from p in players.Values orderby (p.Stats.MaximumHealth + p.Stats.MaximumMana * 2) descending where p.IsLoggedIn select p;
                else
                    return from p in players.Values orderby p.Name where p.IsLoggedIn select p;
            }
        }

        public void AddNewClient(ClientProcess process, ClientVersion version = null)
        {
            var player = new Player(process) { Version = version };
            player.PropertyChanged += Player_PropertyChanged;

            if (player.Version == null)
            {
                player.Version = ClientVersionManager.Instance.Versions.First(v => v.Key != "Auto-Detect");
            }

            AddPlayer(player);
            player.Update();
        }

        public void AddPlayer(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var alreadyExists = players.ContainsKey(player.Process.ProcessId);

            players[player.Process.ProcessId] = player;

            if (alreadyExists)
                OnPlayerUpdated(player);
            else
                OnPlayerAdded(player);
        }

        public bool ContainsPlayer(int processId)
        {
            return players.ContainsKey(processId);
        }

        public Player GetPlayer(int processId)
        {
            players.TryGetValue(processId, out var player);

            return player;
        }

        public Player GetPlayerByName(string playerName)
        {
            foreach (var player in players.Values)
                if (string.Equals(player.Name, playerName, StringComparison.OrdinalIgnoreCase))
                    return player;

            return null;
        }

        public bool RemovePlayer(int processId)
        {
            var wasRemoved = players.TryRemove(processId, out var removedPlayer);

            if (wasRemoved)
            {
                OnPlayerRemoved(removedPlayer);
                removedPlayer.Dispose();
            }

            return wasRemoved;
        }

        public void ClearPlayers()
        {
            var keys = players.Keys.ToList();

            foreach (var key in keys)
                RemovePlayer(key);

            players.Clear();
        }

        public void UpdateClients(Predicate<Player> predicate = null)
        {
            foreach (var client in players.Values)
            {
                try
                {
                    if (predicate == null || predicate(client))
                        client.Update();
                }
                catch { }
            }
        }

        public void RaisePropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void OnPlayerAdded(Player player)
        {
            player.PropertyChanged += Player_PropertyChanged;
            PlayerAdded?.Invoke(this, new PlayerEventArgs(player));
        }

        private void OnPlayerPropertyChanged(Player player, string propertyName) => PlayerPropertyChanged?.Invoke(player, new PropertyChangedEventArgs(propertyName));

        private void OnPlayerUpdated(Player player)
        {
            player.PropertyChanged += Player_PropertyChanged;
            PlayerUpdated?.Invoke(this, new PlayerEventArgs(player));
        }

        private void OnPlayerRemoved(Player player)
        {
            player.PropertyChanged -= Player_PropertyChanged;
            PlayerRemoved?.Invoke(this, new PlayerEventArgs(player));
        }

        private void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is Player player))
                return;

            OnPlayerPropertyChanged(player, e.PropertyName);
        }
    }
}
