using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

using SleepHunter.IO.Process;
using SleepHunter.Settings;

namespace SleepHunter.Models
{
    public sealed class PlayerManager : INotifyPropertyChanged
    {
        #region Singleton
        static readonly PlayerManager instance = new PlayerManager();

        public static PlayerManager Instance
        {
            get { return instance; }
        }

        private PlayerManager() { }
        #endregion

        private readonly ConcurrentDictionary<int, Player> players = new ConcurrentDictionary<int, Player>();
        private PlayerSortOrder sortOrder = PlayerSortOrder.LoginTime;
        private bool showAllClients;

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

        public bool ShowAllClients
        {
            get => showAllClients;
            set => showAllClients = value;
        }

        public IEnumerable<Player> AllClients
        {
            get { return from p in players.Values orderby p.IsLoggedIn descending, p.Name, p.Process.ProcessId select p; }
        }

        public IEnumerable<Player> LoggedInPlayers
        {
            get { return from p in players.Values orderby p.Name where p.IsLoggedIn select p; }
        }

        public IEnumerable<Player> VisiblePlayers
        {
            get
            {
                var sortedClients = Enumerable.Empty<Player>();

                if (SortOrder == PlayerSortOrder.LoginTime)
                    sortedClients = from p in LoggedInPlayers orderby p.LoginTimestamp?.Ticks ?? p.Process.ProcessId select p;
                else if (SortOrder == PlayerSortOrder.Alphabetical)
                    sortedClients = from p in LoggedInPlayers orderby p.Name select p;
                else if (SortOrder == PlayerSortOrder.HighestHealth)
                    sortedClients = from p in LoggedInPlayers orderby p.Stats.MaximumHealth descending select p;
                else if (SortOrder == PlayerSortOrder.HighestMana)
                    sortedClients = from p in LoggedInPlayers orderby p.Stats.MaximumMana descending select p;
                else if (SortOrder == PlayerSortOrder.HighestCombined)
                    sortedClients = from p in LoggedInPlayers orderby (p.Stats.MaximumHealth + p.Stats.MaximumMana * 2) descending select p;
                else
                    sortedClients = from p in LoggedInPlayers orderby p.Name where p.IsLoggedIn select p;

                var visiblePlayers = sortedClients.ToList();

                if (ShowAllClients)
                    visiblePlayers.AddRange(AllClients.Where(c => !c.IsLoggedIn).OrderBy(c => c.Process.CreationTime.Ticks));

                return visiblePlayers;
            }
        }

        public void AddNewClient(ClientProcess process, ClientVersion version = null)
        {
            var player = new Player(process) { Version = version };
            player.PropertyChanged += Player_PropertyChanged;

            if (player.Version == null)
            {
                player.Version = ClientVersionManager.Instance.Versions.FirstOrDefault(v => v.Key != "Auto-Detect");
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
