using System;
using System.Collections.Generic;
using System.Text;

namespace LoopDeLoop.Network
{
    class Game
    {

        public Game(string ownerName)
        {
            this.OwnerName = ownerName;
        }

        public string OwnerName;

        public List<Player> Players = new List<Player>();

        public List<Player> Observers = new List<Player>();

        public Player Generator;

        public Lobby Lobby;

        public Profile Profile;

    }
}
