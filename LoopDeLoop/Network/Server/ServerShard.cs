using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Security;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace LoopDeLoop.Network.Server
{
    public class SavedServerDetails
    {
        public int PortNumber;
    }

    class ServerShard : Shard
    {
        public ServerShard()
        {
            Lobbies.Add("/", new ServerLobby("/", this));
            CspParameters cspParam = new CspParameters();
            cspParam.KeyContainerName = "LoopDeLoopServerKey";
            RSA = new RSACryptoServiceProvider(4096,cspParam);
            RNG = RandomNumberGenerator.Create();
        }

        public object CryptoLock = new object();

        public RandomNumberGenerator RNG;

        public RSACryptoServiceProvider RSA;

        public int PortNumber
        {
            get
            {
                return portNumber;
            }
            set
            {
                portNumber = value;
                SaveSettings();
            }
        }
        private int portNumber = 1331;

        public string StorageFile
        {
            get
            {
                return storageFile;
            }
            set
            {
                storageFile = value;
                SaveSettings();
            }
        }
        private string storageFile;

        private void SaveSettings()
        {
            if (storageFile != null)
            {
                SavedServerDetails saver = new SavedServerDetails();
                saver.PortNumber = this.portNumber;
                XmlSerializer serializer = new XmlSerializer(typeof(SavedServerDetails));
                using (FileStream stream = File.Create(StorageFile))
                {
                    serializer.Serialize(stream, saver);
                }
            }
        }

        public void LoadFromSettings(string storageFile)
        {
            this.storageFile = storageFile;
            XmlSerializer serializer = new XmlSerializer(typeof(SavedServerDetails));
            using (FileStream stream = File.OpenRead(storageFile))
            {
                SavedServerDetails saver = (SavedServerDetails)serializer.Deserialize(stream);
                this.portNumber = saver.PortNumber;
            }
            string basePath = Path.GetDirectoryName(storageFile);
            string[] subs = Directory.GetDirectories(basePath);
            XmlSerializer serializer2 = new XmlSerializer(typeof(Player));
            foreach (string sub in subs)
            {
                string[] children = Directory.GetFiles(sub, "*.xml");
                foreach (string playerFile in children)
                {
                    try
                    {
                        Player newPlayer;
                        using (FileStream stream = File.OpenRead(playerFile))
                        {
                            newPlayer = (Player)serializer2.Deserialize(stream);
                        }
                        lock (Players)
                        {
                            Players.Add(newPlayer.Name, newPlayer);
                        }
                    }
                    catch (Exception e)
                    {
                        Log(e.ToString());
                    }
                }
            }

        }

        private void SavePlayer(Player details)
        {
            if (storageFile != null)
            {
                string name = details.Name;
                string basePath = Path.GetDirectoryName(storageFile);
                string subPath = Path.Combine(basePath, name.Substring(0, 1));
                if (!Directory.Exists(subPath))
                    Directory.CreateDirectory(subPath);
                string fullPath = Path.Combine(subPath, name + ".xml");
                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                using (FileStream stream = File.Create(fullPath))
                {
                    serializer.Serialize(stream, details);
                }
            }
        }

        public void AddLobby(ServerLobby newLobby)
        {
            lock (Lobbies)
            {
                Lobbies.Add(newLobby.Name, newLobby);
            }

            // Broadcast(new LobbyCreatedBroadcastMessage(newLobby.Name));
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

        public Dictionary<string, Player> Players = new Dictionary<string, Player>();

        internal bool CreatePlayer(string name, byte[] passwordHashInput)
        {
            lock (Players)
            {
                if (Players.ContainsKey(name))
                    return false;
                byte[] salt;
                byte[] passwordHash;
                lock (CryptoLock)
                {
                    salt = new byte[32];
                    RNG.GetBytes(salt);
                }
                passwordHash = CreateHash(passwordHashInput, salt);
                Player player = new Player(name, passwordHash, salt);
                Players.Add(name, player);
                SavePlayer(player);
                return true;
            }
        }

        private byte[] CreateHash(byte[] passwordHashInput, byte[] salt)
        {
            SHA256 hasher = SHA256.Create();
            byte[] combined = new byte[passwordHashInput.Length + salt.Length];
            Array.Copy(salt, combined, salt.Length);
            Array.Copy(passwordHashInput, 0, combined, salt.Length, passwordHashInput.Length);
            return hasher.ComputeHash(combined);
        }

        internal bool LoginPlayer(string name, byte[] passwordHashInput, Connection connection)
        {
            Player player;
            bool exists;
            lock (Players)
            {
                exists = Players.TryGetValue(name, out player);
            }
            if (exists)
            {
                byte[] testHash = CreateHash(passwordHashInput, player.Salt);
                bool match = true;
                for (int i = 0; i < player.PasswordHash.Length; i++)
                {
                    if (player.PasswordHash[i] != testHash[i])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    player.Connection = connection;
                    connection.Player = player;
                    Thread sender = new Thread(SendInitialMessages);
                    sender.Start(player);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        private void SendInitialMessages(object objPlayer)
        {
            Player player = (Player)objPlayer;
            List<string> lobbies = new List<string>();
            lock (Lobbies)
            {
                foreach (KeyValuePair<string, Lobby> kvp in Lobbies)
                    lobbies.Add(kvp.Key);
            }
            player.Connection.PostBroadcast(new LobbyExistsBroadcast(lobbies));
            EnterLobby(player, "/");
        }

        private void EnterLobby(Player player, string lobbyName)
        {
            ServerLobby lobby;
            lock (Lobbies)
            {
                lobby = (ServerLobby)Lobbies[lobbyName];
            }
            lobby.AddPlayer(player);
        }

        private Socket serverSocket;

        public void Start()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket = socket;
            socket.Bind(new IPEndPoint(IPAddress.Any, portNumber));
            socket.NoDelay = true;
            socket.Listen(100);
            socket.BeginAccept(new AsyncCallback(ConnectionAccept),socket);
        }

        internal void Stop()
        {
            try
            {
                serverSocket.Close(500);
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        private void ConnectionAccept(IAsyncResult res)
        {
            try
            {
                Socket socket = (Socket)res.AsyncState;
                Socket newSocket = socket.EndAccept(res);
                Connection con = new Connection(newSocket, this);
                con.Start();
                socket.BeginAccept(ConnectionAccept, socket);
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        internal void ChangeLobby(string lobbyName, Player player)
        {
            ServerLobby lobby = (ServerLobby)player.Lobby;
            if (lobby != null)
            {
                lobby.RemovePlayer(player);
            }
            EnterLobby(player, lobbyName);
         
        }

        internal override void ConnectionClosed(Connection connection)
        {
            if (connection.Player != null)
            {
                if (connection.Player.Game != null)
                {
                    ((ServerGame)connection.Player.Game).RemovePlayer(connection.Player);
                }
                if (connection.Player.Lobby != null)
                {
                    ((ServerLobby)connection.Player.Lobby).RemovePlayer(connection.Player);
                }
                connection.Player.Connection = null;
            }
            base.ConnectionClosed(connection);
        }
    }
}
