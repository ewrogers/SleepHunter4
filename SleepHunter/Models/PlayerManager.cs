using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;

using SleepHunter.IO.Process;
using SleepHunter.Settings;

namespace SleepHunter.Models
{
    public sealed class PlayerManager
    {
        #region Singleton
        static readonly PlayerManager instance = new PlayerManager();

        public static PlayerManager Instance
        {
            get { return instance; }
        }

        private PlayerManager() { }
        #endregion

        ConcurrentDictionary<int, Player> players = new ConcurrentDictionary<int, Player>();

        public event PlayerEventHandler PlayerAdded;
        public event PropertyChangedEventHandler PlayerPropertyChanged;
        public event PlayerEventHandler PlayerUpdated;
        public event PlayerEventHandler PlayerRemoved;

        public int Count { get { return players.Count; } }

        public IEnumerable<Player> Players
        {
            get { return from p in players.Values orderby p.IsLoggedIn descending, p.Name, p.Process.ProcessId select p; }
        }

        public IEnumerable<Player> LoggedInPlayers
        {
            get { return from p in players.Values orderby p.Name where p.IsLoggedIn select p; }
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
                throw new ArgumentNullException("player");

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
            Player player = null;

            players.TryGetValue(processId, out player);

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
            Player removedPlayer;

            var wasRemoved = players.TryRemove(processId, out removedPlayer);

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

        void OnPlayerAdded(Player player)
        {
            player.PropertyChanged += Player_PropertyChanged;

            var handler = this.PlayerAdded;

            if (handler != null)
                handler(this, new PlayerEventArgs(player));
        }

        void OnPlayerPropertyChanged(Player player, string propertyName)
        {
            var handler = this.PlayerPropertyChanged;

            if (handler != null)
                handler(player, new PropertyChangedEventArgs(propertyName));
        }

        void OnPlayerUpdated(Player player)
        {
            player.PropertyChanged += Player_PropertyChanged;

            var handler = this.PlayerUpdated;

            if (handler != null)
                handler(this, new PlayerEventArgs(player));
        }

        void OnPlayerRemoved(Player player)
        {
            player.PropertyChanged -= Player_PropertyChanged;

            var handler = this.PlayerRemoved;

            if (handler != null)
                handler(this, new PlayerEventArgs(player));
        }

        void Player_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var player = sender as Player;
            if (player == null)
                return;

            OnPlayerPropertyChanged(player, e.PropertyName);
        }
    }
}
