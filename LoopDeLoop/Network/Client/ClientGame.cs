using System;
using System.Collections.Generic;
using System.Text;

namespace LoopDeLoop.Network.Client
{
    class ClientGame : Game
    {
        public ClientGame(string ownerName)
            : base(ownerName)
        {
        }

        internal void UpdateDetails(List<string> players, List<string> observers)
        {
            lock (Players)
            {
                this.Players.Clear();
                foreach (string player in players)
                {
                    this.Players.Add(new Player(player));
                }
            }
            lock (Observers)
            {
                this.Observers.Clear();
                foreach (string observer in observers)
                {
                    this.Observers.Add(new Player(observer));
                }
            }
        }

        internal void PlayerJoined(string playerName, bool playing)
        {
            if (playing)
            {
                lock (Players)
                {
                    bool contains = false;
                    foreach (Player player in Players)
                    {
                        if (player.Name == playerName)
                            contains = true;
                    }
                    if (!contains)
                        this.Players.Add(new Player(playerName));
                }
            }
            else
            {
                lock (Observers)
                {
                    bool contains = false;
                    foreach (Player observer in Observers)
                    {
                        if (observer.Name == playerName)
                            contains = true;
                    }
                    if (!contains)
                        this.Observers.Add(new Player(playerName));
                }
            }
        }

        internal void PlayerExited(string playerName)
        {
            lock (Players)
            {
                Player toRemove = null;
                foreach (Player player in Players)
                {
                    if (player.Name == playerName)
                        toRemove = player;
                }
                if (playerName != null)
                    Players.Remove(toRemove);
            }
            lock (Observers)
            {
                Player toRemove = null;
                foreach (Player player in Observers)
                {
                    if (player.Name == playerName)
                        toRemove = player;
                }
                if (playerName != null)
                    Observers.Remove(toRemove);
            }
        }
    }
}
