using System;
using System.Collections.Generic;
using System.Text;

namespace LoopDeLoop.Network.Client
{
    class ClientLobby : Lobby
    {
        public ClientLobby(string name, ClientShard parentShard)
            : base(name, parentShard)
        {
        }

        internal void AddPlayer(string playerName)
        {
            lock (Players)
            {
                if (!Players.ContainsKey(playerName))
                    Players.Add(playerName, new Player(playerName));
            }
        }

        public void ClearPlayers()
        {
            lock (Players)
            {
                Players.Clear();
            }
        }


        internal void RemovePlayer(string playerName)
        {
            lock (Players)
            {
                if (Players.ContainsKey(playerName))
                    Players.Remove(playerName);
            }
        }

        internal void ClearGames()
        {
            lock (Games)
            {
                Games.Clear();
            }
        }

        internal void AddGame(string gameOwnerName)
        {
            lock (Games)
            {
                if (!Games.ContainsKey(gameOwnerName))
                {
                    ClientGame game = new ClientGame(gameOwnerName);
                    game.Lobby = this;
                    Games.Add(gameOwnerName, game);
                }
            }
        }

        internal void RemoveGame(string gameOwnerName)
        {
            lock (Games)
            {
                if (Games.ContainsKey(gameOwnerName))
                    Games.Remove(gameOwnerName);
            }
        }

        internal void UpdateGameDetails(List<string> players, List<string> observers, string gameOwner)
        {
            lock (Games)
            {
                if (Games.ContainsKey(gameOwner))
                {
                    ClientGame game = (ClientGame)Games[gameOwner];
                    game.UpdateDetails(players, observers);
                }
            }
        }

        internal void PlayerJoinedGame(string playerName, string gameOwner, bool playing)
        {
            lock (Games)
            {
                if (Games.ContainsKey(gameOwner))
                {
                    ClientGame game = (ClientGame)Games[gameOwner];
                    game.PlayerJoined(playerName, playing);
                }
            }
        }

        internal void PlayerExitedGame(string playerName, string gameOwner)
        {
            lock (Games)
            {
                if (Games.ContainsKey(gameOwner))
                {
                    ClientGame game = (ClientGame)Games[gameOwner];
                    game.PlayerExited(playerName);
                }
            }
        }
    }
}
