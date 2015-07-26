using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace LoopDeLoop.Network
{
    public class Player
    {
        public Player()
        {
        }
        public Player(string name)
        {
            Name = name;
        }
        public Player(string name, byte[] passwordHash, byte[] salt)
        {
            Name = name;
            PasswordHash = passwordHash;
            Salt = salt;
        }

        public string Name;
        public byte[] Salt;
        public byte[] PasswordHash;

        [XmlIgnore]
        internal Lobby Lobby;

        [XmlIgnore]
        internal Game Game;

        [XmlIgnore]
        internal Connection Connection
        {
            get
            {
                return connection;
            }
            set
            {
                connection = value;
            }
        }
        private Connection connection;
    }
}
