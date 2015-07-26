using System;
using System.Collections.Generic;
using System.Text;

namespace LoopDeLoop.Network.Server
{
    class ServerLobby : Lobby
    {
        public ServerLobby(string name, ServerShard parentShard)
            : base(name, parentShard)
        {
        }

        public void Broadcast(Message msg)
        {
            Player[] players;
            lock (Players)
            {
                players = new Player[Players.Count];
                Players.Values.CopyTo(players, 0);
            }
            foreach (Player player in players)
            {
                try
                {
                    if (player.Connection != null)
                        player.Connection.PostBroadcast(msg);
                }
                catch
                {
                }
            }
        }

        internal void AddPlayer(Player player)
        {
            List<string> players = new List<string>();
            lock (Players)
            {
                if (!Players.ContainsKey(player.Name))
                    Players.Add(player.Name, player);
                foreach (KeyValuePair<string, Player> kvp in Players)
                    players.Add(kvp.Key);
                player.Lobby = this;
            }
            List<string> games = new List<string>();
            lock (Games)
            {
                foreach (KeyValuePair<string, Game> kvp in Games)
                {
                    games.Add(kvp.Key);
                }
            }
            player.Connection.PostBroadcast(new PlayerExistsInLobbyBroadcast(players, Name));
            player.Connection.PostBroadcast(new GameExistsBroadcast(games, Name));
            Broadcast(new PlayerEnteredLobbyBroadcast(player.Name, Name));
        }


        internal void RemovePlayer(Player player)
        {
            lock (Players)
            {
                if (Players.ContainsKey(player.Name))
                    Players.Remove(player.Name);
                player.Lobby = null;
            }
            Broadcast(new PlayerExitedLobbyBroadcast(player.Name, Name));
        }

        internal void AddGame(string gameOwnerName)
        {
            ServerGame game = new ServerGame(gameOwnerName);
            Player owner;
            lock (Players)
            {
                if (!Players.ContainsKey(gameOwnerName))
                    return;
                owner = Players[gameOwnerName];
            }
            lock (Games)
            {
                if (!Games.ContainsKey(gameOwnerName))
                    Games.Add(gameOwnerName, game);
            }
            game.Lobby = this;
            Broadcast(new GameCreatedBroadcast(gameOwnerName, Name));
            game.AddPlayer(owner);
        }

        internal void RemoveGame(string gameOwnerName)
        {
            lock (Games)
            {
                if (Games.ContainsKey(gameOwnerName))
                    Games.Remove(gameOwnerName);
            }
            Broadcast(new GameEndedBroadcast(gameOwnerName, Name));
        }

        internal bool JoinGame(Player player, string ownerName, bool playing)
        {
            lock (Games)
            {
                if (Games.ContainsKey(ownerName)) {
                    ServerGame game = (ServerGame)Games[ownerName];

                    // TODO: check if game is locked.
                    if (playing)
                        game.AddPlayer(player);
                    else
                        game.AddObserver(player);
                    return true;
                }
            }
            return false;
        }
    }
}
