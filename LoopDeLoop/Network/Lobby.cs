using System;
using System.Collections.Generic;
using System.Text;

namespace LoopDeLoop.Network
{
    abstract class Lobby
    {
        public Lobby(string name, Shard parentShard)
        {
            this.Name = name;
            this.ParentShard = parentShard;
        }

        public string Name;

        public Shard ParentShard;

        public Dictionary<string, Player> Players = new Dictionary<string, Player>();

        public Dictionary<string, Game> Games = new Dictionary<string, Game>();
     }
}
