using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace LoopDeLoop.Network
{

    enum MessageType
    {
        None,
        CreatePlayer,
        CreatePlayerResponse,
        LoginPlayer,
        LoginPlayerResponse,
        SearchForPlayer,
        SearchForPlayerResponse,
        LobbyChat,
        LobbyChatBroadcast,
        LobbyCreatedBroadcast,
        LobbyDeletedBroadcast,
        LobbyExistsBroadcast,
        ChangeLobby,
        ChangeLobbyResponse,
        PlayerEnteredLobbyBroadcast,
        PlayerExitedLobbyBroadcast,
        PlayerExistsInLobbyBroadcast,
        NewGame,
        NewGameResponse,
        JoinGame,
        JoinGameResponse,
        ExitGame,
        ExitGameResponse,
        GameCreatedBroadcast,
        GameEndedBroadcast,
        GameExistsBroadcast,
        GameLockedBroadcast,
        PlayerEnteredGameBroadcast,
        PlayerExitedGameBroadcast,
        PlayerExistsInGameBroadcast,
        GameChat,
        GameChatBroadcast,
        LockGame,
        LockGameResponse,
        AcceptGame,
        AcceptGameResponse,
        AcceptCountBroadcast,
        StartingGameBroadcast,
        GenerateRequest,
        GenerateResponse,
        MakeMove,
        MakeMoveResponse,
        MakeMoveBroadcast,
        ScoresBroadcast,
        BoardDetailsBroadcast,
        Hello,
        HelloResponse,
        ProfileDetailsBroadcast,
        ProfileDetails,
        ProfileDetailsResponse,
    }

    class Connection
    {

        public Connection(Socket socket, Shard shard)
            : this(socket)
        {
            Shard = shard;
        }

        public Connection (Socket socket) {
            stream = new NetworkStream(socket);
            writer = new BinaryWriter(stream);
        }

        public void Start()
        {
            readerThread = new Thread(Reader);
            readerThread.IsBackground = true;
            readerThread.Start();
        }

        public static Version ProtocolVersion = new Version(1, 0, 0, 0);

        public byte[] Nonce;

        public Shard Shard;

        public Player Player;

        Thread readerThread;

        NetworkStream stream;

        BinaryWriter writer;

        Dictionary<int, EventWaitHandle> waitHandles = new Dictionary<int, EventWaitHandle>();
        Dictionary<int, Message> responses = new Dictionary<int, Message>();

        public Message SendMessage(Message msg)
        {
            byte[] body = msg.GetBody();
            int msgIndex = GetMessageIndex();
            EventWaitHandle waiter = new ManualResetEvent(false);
            // Have to add waithandle before we send data, incase the response comes back before we add it.
            lock (waitHandles)
            {
                waitHandles.Add(msgIndex, waiter);
            }
            lock (stream)
            {
                writer.Write((int)msg.Type);
                writer.Write(msgIndex);
                writer.Write(body.Length);
                writer.Flush();
                stream.Write(body, 0, body.Length);
            }
            waiter.WaitOne();
            lock (waitHandles)
            {
                waitHandles.Remove(msgIndex);
            }
            lock (responses)
            {
                if (responses.ContainsKey(msgIndex))
                {
                    Message answer = responses[msgIndex];
                    responses.Remove(msgIndex);
                    return answer;
                }
                else
                    throw new IOException("Send failed.");
            }
        }

        public void Reader()
        {
            byte[] buffer = new byte[1024];
            try
            {
                int offset = 0;
                bool done;
                while (true)
                {
                    int next = stream.Read(buffer, offset, buffer.Length - offset);
                    if (next == 0)
                        break;
                    offset += next;
                    done = false;
                    while (!done)
                    {
                        done = true;
                        if (offset >= 12)
                        {
                            MessageType currentMessage = (MessageType)BitConverter.ToInt32(buffer, 0);
                            int messageNumber = BitConverter.ToInt32(buffer, 4);
                            int length = BitConverter.ToInt32(buffer, 8);
                            if (offset >= length + 12)
                            {
                                byte[] messageBody = new byte[length];
                                Array.Copy(buffer, 12, messageBody, 0, length);
                                if (messageNumber >= 0)
                                {
                                    try
                                    {
                                        Message res = Message.CreateMessage(this, currentMessage, messageNumber, messageBody);
                                        res.Process();
                                    }
                                    catch (Exception e)
                                    {
                                        try
                                        {
                                            Shard.Log("Message Processing failed: " + e.ToString());
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        Message res = Message.CreateMessage(this, currentMessage, -messageNumber, messageBody);
                                        int index = -messageNumber;
                                        lock (responses)
                                        {
                                            responses.Add(index, res);
                                        }
                                        lock (waitHandles)
                                        {
                                            waitHandles[index].Set();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        try
                                        {
                                            Shard.Log("Response Processing failed: " + e.ToString());
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                                if (offset > length + 12)
                                {
                                    offset -= length + 12;
                                    for (int i = 0; i < offset; i++)
                                    {
                                        buffer[i] = buffer[i + length + 12];
                                    }
                                    done = false;
                                }
                                else
                                {
                                    offset = 0;
                                }
                            }
                            else
                            {
                                if (buffer.Length < length + 12)
                                {
                                    byte[] newBuffer = new byte[length + 12];
                                    Array.Copy(buffer, newBuffer, offset);
                                    buffer = newBuffer;
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    if (Shard != null)
                        Shard.Log("Connection closed through exception: " + e.ToString());
                }
                catch
                {
                }
            }
            finally
            {
                try
                {
                    // Let all the senders discover we're dead.
                    lock (waitHandles)
                    {
                        foreach (EventWaitHandle waitHandle in waitHandles.Values)
                        {
                            waitHandle.Set();
                        }
                    }
                }
                catch
                {
                }
                try
                {
                    Shard.ConnectionClosed(this);
                }
                catch
                {
                }
            }
        }

        int nextMessageIndex;

        private int GetMessageIndex()
        {
            return Interlocked.Increment(ref nextMessageIndex);
        }

        public void PostBroadcast(Message msg) {
            PostResponse(msg, 0);
        }

        public void PostResponse(Message msg, int messageNumber)
        {
            byte[] body = msg.GetBody();
            lock (stream)
            {
                writer.Write((int)msg.Type);
                writer.Write(-messageNumber);
                writer.Write(body.Length);
                writer.Flush();
                stream.Write(body, 0, body.Length);
            }
        }

        internal void Stop()
        {
            try
            {
                stream.Close();
            }
            catch
            {
            }
            try
            {
                readerThread.Abort();
            }
            catch
            {
            }
        }
    }

    abstract class Message
    {
        public Message(MessageType type)
        {
            Type = type;
        }
        public Message(MessageType type, int messageNumber, Connection connection)
            : this(type)
        {
            MessageNumber = messageNumber;
            Connection = connection;
        }
        public MessageType Type;

        public int MessageNumber;

        public Connection Connection;

        public abstract byte[] GetBody();

        internal static Message CreateMessage(Connection connection, MessageType currentMessage, int messageNumber, byte[] messageBody)
        {
            switch (currentMessage)
            {
                case MessageType.CreatePlayer:
                    return new CreatePlayerMessage(messageNumber, connection, messageBody);
                case MessageType.CreatePlayerResponse:
                    return new CreatePlayerResponseMessage(messageNumber, connection, messageBody);
                case MessageType.LoginPlayer:
                    return new LoginPlayerMessage(messageNumber, connection, messageBody);
                case MessageType.LoginPlayerResponse:
                    return new LoginPlayerResponseMessage(messageNumber, connection, messageBody);
                case MessageType.LobbyExistsBroadcast:
                    return new LobbyExistsBroadcast(messageNumber, connection, messageBody);
                case MessageType.PlayerEnteredLobbyBroadcast:
                    return new PlayerEnteredLobbyBroadcast(messageNumber, connection, messageBody);
                case MessageType.PlayerExitedLobbyBroadcast:
                    return new PlayerExitedLobbyBroadcast(messageNumber, connection, messageBody);
                case MessageType.PlayerExistsInLobbyBroadcast:
                    return new PlayerExistsInLobbyBroadcast(messageNumber, connection, messageBody);
                case MessageType.LobbyChat:
                    return new LobbyChatMessage(messageNumber, connection, messageBody);
                case MessageType.LobbyChatBroadcast:
                    return new LobbyChatBroadcast(messageNumber, connection, messageBody);
                case MessageType.GameChat:
                    return new GameChatMessage(messageNumber, connection, messageBody);
                case MessageType.GameChatBroadcast:
                    return new GameChatBroadcast(messageNumber, connection, messageBody);
                case MessageType.ChangeLobbyResponse:
                    return new ChangeLobbyResponse(messageNumber, connection, messageBody);
                case MessageType.ChangeLobby:
                    return new ChangeLobbyMessage(messageNumber, connection, messageBody);
                case MessageType.NewGame:
                    return new NewGameMessage(messageNumber, connection, messageBody);
                case MessageType.NewGameResponse:
                    return new NewGameResponse(messageNumber, connection, messageBody);
                case MessageType.JoinGame:
                    return new JoinGameMessage(messageNumber, connection, messageBody);
                case MessageType.JoinGameResponse:
                    return new JoinGameResponse(messageNumber, connection, messageBody);
                case MessageType.ExitGame:
                    return new ExitGameMessage(messageNumber, connection, messageBody);
                case MessageType.ExitGameResponse:
                    return new ExitGameResponse(messageNumber, connection, messageBody);
                case MessageType.GameCreatedBroadcast:
                    return new GameCreatedBroadcast(messageNumber, connection, messageBody);
                case MessageType.GameEndedBroadcast:
                    return new GameEndedBroadcast(messageNumber, connection, messageBody);
                case MessageType.GameExistsBroadcast:
                    return new GameExistsBroadcast(messageNumber, connection, messageBody);
                case MessageType.PlayerExistsInGameBroadcast:
                    return new PlayerExistsInGameBroadcast(messageNumber, connection, messageBody);
                case MessageType.PlayerEnteredGameBroadcast:
                    return new PlayerEnteredGameBroadcast(messageNumber, connection, messageBody);
                case MessageType.PlayerExitedGameBroadcast:
                    return new PlayerExitedGameBroadcast(messageNumber, connection, messageBody);
                case MessageType.AcceptGame:
                    return new AcceptGameMessage(messageNumber, connection, messageBody);
                case MessageType.AcceptGameResponse:
                    return new AcceptGameResponse(messageNumber, connection, messageBody);
                case MessageType.StartingGameBroadcast:
                    return new StartingGameBroadcast(messageNumber, connection, messageBody);
                case MessageType.GenerateRequest:
                    return new GenerateMessage(messageNumber, connection, messageBody);
                case MessageType.GenerateResponse:
                    return new GenerateResponse(messageNumber, connection, messageBody);
                case MessageType.BoardDetailsBroadcast:
                    return new BoardDetailsBroadcast(messageNumber, connection, messageBody);
                case MessageType.MakeMove:
                    return new MoveMessage(messageNumber, connection, messageBody);
                case MessageType.MakeMoveResponse:
                    return new MoveResponse(messageNumber, connection, messageBody);
                case MessageType.MakeMoveBroadcast:
                    return new MoveBroadcast(messageNumber, connection, messageBody);
                case MessageType.ScoresBroadcast:
                    return new ScoresBroadcast(messageNumber, connection, messageBody);
                case MessageType.AcceptCountBroadcast:
                    return new AcceptCountBroadcast(messageNumber, connection, messageBody);
                case MessageType.Hello:
                    return new HelloMessage(messageNumber, connection, messageBody);
                case MessageType.HelloResponse:
                    return new HelloResponse(messageNumber, connection, messageBody);
                case MessageType.ProfileDetailsBroadcast:
                    return new ProfileDetailsBroadcast(messageNumber, connection, messageBody);
                case MessageType.ProfileDetails:
                    return new ProfileDetailsMessage(messageNumber, connection, messageBody);
                case MessageType.ProfileDetailsResponse:
                    return new ProfileDetailsResponse(messageNumber, connection, messageBody);
            }
            throw new NotSupportedException("Message Type not recognized.");
        }

        public abstract void Process();
    }

    abstract class UsernamePasswordMessage : Message
    {
        public UsernamePasswordMessage(MessageType messageType, string name, byte[] passwordHash, byte[] publicKeyData, byte[] nonce)
            : base(messageType)
        {
            this.name = name;
            this.passwordHash = passwordHash;
            this.publicKeyData = publicKeyData;
            this.nonce = nonce;
            System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            this.additionalNonce = new byte[32];
            rng.GetBytes(this.additionalNonce);
            this.startNonce = new byte[32];
            rng.GetBytes(this.startNonce);
        }
        public UsernamePasswordMessage(MessageType messageType, int messageNumber, Connection connection, byte[] messageBody)
            : base(messageType, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            Server.ServerShard serverShard = ((Server.ServerShard)connection.Shard);
            byte[] mashed = reader.ReadBytes(reader.ReadInt32());
            byte[] body = reader.ReadBytes(reader.ReadInt32());
            byte[] decrypt;
            lock (serverShard.CryptoLock)
            {
                decrypt = serverShard.RSA.Decrypt(body, false);
            }
            MemoryStream bodyStream = new MemoryStream(decrypt);
            startNonce = new byte[32];
            bodyStream.Read(startNonce, 0, 32);
            nonce = new byte[32];
            bodyStream.Read(nonce, 0, 32);
            passwordHash = new byte[32];
            bodyStream.Read(passwordHash, 0, 32);
            additionalNonce = new byte[32];
            bodyStream.Read(additionalNonce, 0, 32);
            for (int i = 0; i < nonce.Length; i++)
            {
                if (nonce[i] != Connection.Nonce[i])
                    throw new Exception("Invalid login packet.");
            }
            name = Unmash(mashed, additionalNonce);
        }

        private string Unmash(byte[] mashed, byte[] additionalNonce)
        {
            byte[] res = mashed;
            for (int i = 0; i < res.Length; i++)
            {
                int index = i % additionalNonce.Length;
                res[i] ^= additionalNonce[index];
            }
            return Encoding.UTF8.GetString(res);
        }

        protected string name;
        protected byte[] passwordHash;
        private byte[] publicKeyData;
        private byte[] nonce;
        private byte[] additionalNonce;
        private byte[] startNonce;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            byte[] mashed = Mash(name, additionalNonce);
            writer.Write(mashed.Length);
            writer.Write(mashed, 0, mashed.Length);
            System.Security.Cryptography.RSACryptoServiceProvider rsa = new System.Security.Cryptography.RSACryptoServiceProvider();
            rsa.ImportCspBlob(publicKeyData);
            MemoryStream combiner = new MemoryStream();
            combiner.Write(startNonce, 0, startNonce.Length);
            combiner.Write(nonce, 0, nonce.Length);
            combiner.Write(passwordHash, 0, passwordHash.Length);
            combiner.Write(additionalNonce, 0, additionalNonce.Length);
            byte[] toSend = rsa.Encrypt(combiner.ToArray(), false);
            writer.Write(toSend.Length);
            writer.Write(toSend, 0, toSend.Length);
            writer.Flush();
            return stream.ToArray();
        }

        private static byte[] Mash(string name, byte[] nonce)
        {
            byte[] res = Encoding.UTF8.GetBytes(name);
            for (int i = 0; i < res.Length; i++)
            {
                int index = i % nonce.Length;
                res[i] ^= nonce[index];
            }
            return res;
        }
    }

    class CreatePlayerMessage : UsernamePasswordMessage
    {
        public CreatePlayerMessage(string name, byte[] passwordHash, byte[] publicKeyData, byte[] nonce)
            : base(MessageType.CreatePlayer, name, passwordHash, publicKeyData, nonce)
        {
        }
        public CreatePlayerMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.CreatePlayer, messageNumber, connection, messageBody)
        {
        }

        public override void Process()
        {
            bool success = ((Server.ServerShard)Connection.Shard).CreatePlayer(name, passwordHash);
            if (success)
                ((Server.ServerShard)Connection.Shard).LoginPlayer(name, passwordHash, Connection);
            Message response = new CreatePlayerResponseMessage(success);
            Connection.PostResponse(response, MessageNumber);
        }
    }

    class LoginPlayerMessage : UsernamePasswordMessage
    {
        public LoginPlayerMessage(string name, byte[] passwordHash, byte[] publicKeyData, byte[] nonce)
            : base(MessageType.LoginPlayer, name, passwordHash, publicKeyData, nonce)
        {
        }
        public LoginPlayerMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.LoginPlayer, messageNumber, connection, messageBody)
        {
        }

        public override void Process()
        {
            bool success = ((Server.ServerShard)Connection.Shard).LoginPlayer(name, passwordHash, Connection);
            Message response = new LoginPlayerResponseMessage(success);
            Connection.PostResponse(response, MessageNumber);
        }
    }

    abstract class BoolResponseMessage : Message
    {
        public BoolResponseMessage(MessageType messageType, int messageNumber, Connection connection, byte[] messageBody)
            : base(messageType, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            success = reader.ReadBoolean();
        }

        public BoolResponseMessage(MessageType messageType, bool success)
            : base(messageType)
        {
            this.success = success;
        }

        public bool Success
        {
            get
            {
                return success;
            }
        }
        private bool success;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(success);
            writer.Flush();
            return stream.ToArray();

        }

        public override void Process()
        {
            throw new InvalidOperationException("Responses should never be processed.");
        }
     }

    class CreatePlayerResponseMessage : BoolResponseMessage
    {
        public CreatePlayerResponseMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.CreatePlayerResponse, messageNumber, connection, messageBody)
        {
        }

        public CreatePlayerResponseMessage(bool success)
            : base(MessageType.CreatePlayerResponse, success)
        {
        }
    }

    class LoginPlayerResponseMessage : BoolResponseMessage
    {
        public LoginPlayerResponseMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.LoginPlayerResponse, messageNumber, connection, messageBody)
        {
        }

        public LoginPlayerResponseMessage(bool success)
            : base(MessageType.LoginPlayerResponse, success)
        {
        }
    }

    abstract class StringArrayBroadcast : Message
    {
        public StringArrayBroadcast(MessageType messageType, int messageNumber, Connection connection, byte[] messageBody)
            : base(messageType, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            int stringsCount = reader.ReadInt32();
            strings = new List<string>();
            for (int i = 0; i < stringsCount; i++)
            {
                strings.Add(reader.ReadString());
            }
        }

        public StringArrayBroadcast(MessageType messageType, List<string> strings)
            : base(messageType)
        {
            this.strings = strings;
        }

        protected List<string> strings;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(strings.Count);
            foreach (string str in strings)
                writer.Write(str);
            writer.Flush();
            return stream.ToArray();
        }
    }

    class LobbyExistsBroadcast : StringArrayBroadcast
    {
        public LobbyExistsBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.LobbyExistsBroadcast, messageNumber, connection, messageBody)
        {
        }

        public LobbyExistsBroadcast(List<string> lobbies)
            : base(MessageType.LobbyExistsBroadcast, lobbies)
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).AddLobbies(strings);
        }
    }

    class PlayerEnteredLobbyBroadcast : StringArrayBroadcast
    {
        public PlayerEnteredLobbyBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.PlayerEnteredLobbyBroadcast, messageNumber, connection, messageBody)
        {
        }

        public PlayerEnteredLobbyBroadcast(string playerName, string lobbyName)
            : base(MessageType.PlayerEnteredLobbyBroadcast, FormList(playerName, lobbyName))
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).AddPlayerToLobby(strings[0], strings[1]);
        }

        private static List<string> FormList(string playerName, string lobbyName)
        {
            List<string> strings = new List<string>();
            strings.Add(playerName);
            strings.Add(lobbyName);
            return strings;
        }
    }

    class PlayerExitedLobbyBroadcast : StringArrayBroadcast
    {
        public PlayerExitedLobbyBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.PlayerExitedLobbyBroadcast, messageNumber, connection, messageBody)
        {
        }

        public PlayerExitedLobbyBroadcast(string playerName, string lobbyName)
            : base(MessageType.PlayerExitedLobbyBroadcast, FormList(playerName, lobbyName))
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).RemovePlayerFromLobby(strings[0], strings[1]);
        }

        private static List<string> FormList(string playerName, string lobbyName)
        {
            List<string> strings = new List<string>();
            strings.Add(playerName);
            strings.Add(lobbyName);
            return strings;
        }
    }

    class PlayerExistsInLobbyBroadcast : StringArrayBroadcast
    {
        public PlayerExistsInLobbyBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.PlayerExistsInLobbyBroadcast, messageNumber, connection, messageBody)
        {
        }

        public PlayerExistsInLobbyBroadcast(List<string> playerNames, string lobbyName)
            : base(MessageType.PlayerExistsInLobbyBroadcast, FormList(playerNames, lobbyName))
        {
        }

        public override void Process()
        {
            List<string> playerNames = new List<string>();
            for (int i = 0; i < strings.Count - 1; i++)
                playerNames.Add(strings[i]);
            ((Client.ClientShard)Connection.Shard).AddPlayersToLobby(playerNames, strings[strings.Count - 1]);
        }

        private static List<string> FormList(List<string> playerNames, string lobbyName)
        {
            List<string> strings = new List<string>();
            strings.AddRange(playerNames);
            strings.Add(lobbyName);
            return strings;
        }
    }

    class LobbyChatMessage : StringArrayBroadcast
    {
        public LobbyChatMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.LobbyChat, messageNumber, connection, messageBody)
        {
        }

        public LobbyChatMessage(string message)
            : base(MessageType.LobbyChat, FormList(message))
        {
        }

        public override void Process()
        {
            if (Connection.Player.Lobby != null)
                ((Server.ServerLobby)Connection.Player.Lobby).Broadcast(new LobbyChatBroadcast(strings[0], Connection.Player.Name));
        }

        private static List<string> FormList(string message)
        {
            List<string> strings = new List<string>();
            strings.Add(message);
            return strings;
        }
    }

    class LobbyChatBroadcast : StringArrayBroadcast
    {
        public LobbyChatBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.LobbyChatBroadcast, messageNumber, connection, messageBody)
        {
        }

        public LobbyChatBroadcast(string message, string playerName)
            : base(MessageType.LobbyChatBroadcast, FormList(message, playerName))
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).ReceiveLobbyChat(strings[0], strings[1]);
        }

        private static List<string> FormList(string message, string playerName)
        {
            List<string> strings = new List<string>();
            strings.Add(message);
            strings.Add(playerName);
            return strings;
        }
    }

    class GameChatMessage : StringArrayBroadcast
    {
        public GameChatMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.GameChat, messageNumber, connection, messageBody)
        {
        }

        public GameChatMessage(string message)
            : base(MessageType.GameChat, FormList(message))
        {
        }

        public override void Process()
        {
            if (Connection.Player.Game != null)
                ((Server.ServerGame)Connection.Player.Game).Broadcast(new GameChatBroadcast(strings[0], Connection.Player.Name));
        }

        private static List<string> FormList(string message)
        {
            List<string> strings = new List<string>();
            strings.Add(message);
            return strings;
        }
    }

    class GameChatBroadcast : StringArrayBroadcast
    {
        public GameChatBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.GameChatBroadcast, messageNumber, connection, messageBody)
        {
        }

        public GameChatBroadcast(string message, string playerName)
            : base(MessageType.GameChatBroadcast, FormList(message, playerName))
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).ReceiveGameChat(strings[0], strings[1]);
        }

        private static List<string> FormList(string message, string playerName)
        {
            List<string> strings = new List<string>();
            strings.Add(message);
            strings.Add(playerName);
            return strings;
        }
    }

    class ChangeLobbyMessage : StringArrayBroadcast
    {
        public ChangeLobbyMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ChangeLobby, messageNumber, connection, messageBody)
        {
        }

        public ChangeLobbyMessage(string newLobby)
            : base(MessageType.ChangeLobby, FormList(newLobby))
        {
        }

        private static List<string> FormList(string newLobby)
        {
            List<string> strings = new List<string>();
            strings.Add(newLobby);
            return strings;
        }

        public override void Process()
        {
            ((Server.ServerShard)Connection.Shard).ChangeLobby(strings[0], Connection.Player);
            Message response = new ChangeLobbyResponse(true);
            Connection.PostResponse(response, MessageNumber);
        }
    }

    class ChangeLobbyResponse : BoolResponseMessage
    {
        public ChangeLobbyResponse(bool success)
            : base(MessageType.ChangeLobbyResponse, success)
        {
        }

        public ChangeLobbyResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ChangeLobbyResponse, messageNumber, connection, messageBody)
        {
        }
    }

    class NewGameMessage : Message
    {
        public NewGameMessage()
            : base(MessageType.NewGame)
        {
        }
        public NewGameMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.NewGame, messageNumber, connection)
        {
        }

        public override byte[] GetBody()
        {
            return new byte[0];
        }

        public override void Process()
        {
            ((Server.ServerLobby)Connection.Player.Lobby).AddGame(Connection.Player.Name);
            Connection.PostResponse(new NewGameResponse(true), MessageNumber);
        }
    }

    class NewGameResponse : BoolResponseMessage
    {
        public NewGameResponse(bool success)
            : base(MessageType.NewGameResponse, success)
        {
        }

        public NewGameResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.NewGameResponse, messageNumber, connection, messageBody)
        {
        }
    }

    class JoinGameMessage : Message
    {
        public JoinGameMessage(string ownerName, bool player)
            : base(MessageType.JoinGame)
        {
            this.ownerName = ownerName;
            this.player = player;
        }
        public JoinGameMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.JoinGame, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            ownerName = reader.ReadString();
            player = reader.ReadBoolean();
        }

        private string ownerName;
        private bool player;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(ownerName);
            writer.Write(player);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            bool success = ((Server.ServerLobby)Connection.Player.Lobby).JoinGame(Connection.Player, ownerName, player);
            Connection.PostResponse(new JoinGameResponse(success), MessageNumber);
        }
    }

    class JoinGameResponse : BoolResponseMessage
    {
        public JoinGameResponse(bool success)
            : base(MessageType.JoinGameResponse, success)
        {
        }

        public JoinGameResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.JoinGameResponse, messageNumber, connection, messageBody)
        {
        }
    }

    class ExitGameMessage : Message
    {
        public ExitGameMessage()
            : base(MessageType.ExitGame)
        {
        }
        public ExitGameMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ExitGame, messageNumber, connection)
        {
        }

        public override byte[] GetBody()
        {
            return new byte[0];
        }

        public override void Process()
        {
            ((Server.ServerGame)Connection.Player.Game).RemovePlayer(Connection.Player);
            Connection.PostResponse(new ExitGameResponse(true), MessageNumber);
        }
    }

    class ExitGameResponse : BoolResponseMessage
    {
        public ExitGameResponse(bool success)
            : base(MessageType.ExitGameResponse, success)
        {
        }

        public ExitGameResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ExitGameResponse, messageNumber, connection, messageBody)
        {
        }
    }

    class GameExistsBroadcast : StringArrayBroadcast
    {
        public GameExistsBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.GameExistsBroadcast, messageNumber, connection, messageBody)
        {
        }

        public GameExistsBroadcast(List<string> gameOwnerNames, string lobbyName)
            : base(MessageType.GameExistsBroadcast, FormList(gameOwnerNames, lobbyName))
        {
        }

        public override void Process()
        {
            List<string> gameOwnerNames = new List<string>();
            for (int i = 0; i < strings.Count - 1; i++)
                gameOwnerNames.Add(strings[i]);
            ((Client.ClientShard)Connection.Shard).AddGamesToLobby(gameOwnerNames, strings[strings.Count - 1]);
        }

        private static List<string> FormList(List<string> gameOwnerNames, string lobbyName)
        {
            List<string> strings = new List<string>();
            strings.AddRange(gameOwnerNames);
            strings.Add(lobbyName);
            return strings;
        }
    }


    class GameCreatedBroadcast : StringArrayBroadcast
    {
        public GameCreatedBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.GameCreatedBroadcast, messageNumber, connection, messageBody)
        {
        }

        public GameCreatedBroadcast(string ownerName, string lobbyName)
            : base(MessageType.GameCreatedBroadcast, FormList(ownerName, lobbyName))
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).AddGameToLobby(strings[0], strings[1]);
        }

        private static List<string> FormList(string ownerName, string lobbyName)
        {
            List<string> strings = new List<string>();
            strings.Add(ownerName);
            strings.Add(lobbyName);
            return strings;
        }
    }

    class GameEndedBroadcast : StringArrayBroadcast
    {
        public GameEndedBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.GameEndedBroadcast, messageNumber, connection, messageBody)
        {
        }

        public GameEndedBroadcast(string ownerName, string lobbyName)
            : base(MessageType.GameEndedBroadcast, FormList(ownerName, lobbyName))
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).RemoveGameFromLobby(strings[0], strings[1]);
        }

        private static List<string> FormList(string ownerName, string lobbyName)
        {
            List<string> strings = new List<string>();
            strings.Add(ownerName);
            strings.Add(lobbyName);
            return strings;
        }
    }

    class PlayerExistsInGameBroadcast : Message
    {
        public PlayerExistsInGameBroadcast(List<string> players, List<string> observers, string gameOwner)
            : base(MessageType.PlayerExistsInGameBroadcast)
        {
            this.players = players;
            this.observers = observers;
            this.gameOwner = gameOwner;
        }
        public PlayerExistsInGameBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.PlayerExistsInGameBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            int playerCount = reader.ReadInt32();
            players = new List<string>();
            for (int i = 0; i < playerCount; i++)
                players.Add(reader.ReadString());
            observers = new List<string>();
            int observerCount = reader.ReadInt32();
            for (int i = 0; i < observerCount; i++)
            {
                observers.Add(reader.ReadString());
            }
            gameOwner = reader.ReadString();
        }

        private List<string> players;
        private List<string> observers;
        private string gameOwner;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(players.Count);
            foreach (string player in players)
                writer.Write(player);
            writer.Write(observers.Count);
            foreach (string observer in observers)
                writer.Write(observer);

            writer.Write(gameOwner);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).UpdateGameDetails(players, observers, gameOwner);
        }
    }

    class PlayerEnteredGameBroadcast : Message
    {
        public PlayerEnteredGameBroadcast(string playerName, string gameOwner, bool playing)
            : base(MessageType.PlayerEnteredGameBroadcast)
        {
            this.playerName = playerName;
            this.gameOwner = gameOwner;
            this.playing = playing;
        }
        public PlayerEnteredGameBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.PlayerEnteredGameBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            playerName = reader.ReadString();
            gameOwner = reader.ReadString();
            playing = reader.ReadBoolean();
        }

        private string playerName;
        private string gameOwner;
        private bool playing;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(playerName);
            writer.Write(gameOwner);
            writer.Write(playing);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).PlayerJoinedGame(playerName, gameOwner, playing);
        }
    }

    class PlayerExitedGameBroadcast : Message
    {
        public PlayerExitedGameBroadcast(string playerName, string gameOwner)
            : base(MessageType.PlayerExitedGameBroadcast)
        {
            this.playerName = playerName;
            this.gameOwner = gameOwner;
        }
        public PlayerExitedGameBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.PlayerExitedGameBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            playerName = reader.ReadString();
            gameOwner = reader.ReadString();
        }

        private string playerName;
        private string gameOwner;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(playerName);
            writer.Write(gameOwner);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).PlayerLeftGame(playerName, gameOwner);
        }
    }
    class AcceptGameMessage : Message
    {
        public AcceptGameMessage()
            : base(MessageType.AcceptGame)
        {
        }
        public AcceptGameMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.AcceptGame, messageNumber, connection)
        {
        }

        public override byte[] GetBody()
        {
            return new byte[0];
        }

        public override void Process()
        {
            ((Server.ServerGame)Connection.Player.Game).AcceptGame(Connection.Player);
            Connection.PostResponse(new AcceptGameResponse(true), MessageNumber);
        }
    }

    class AcceptGameResponse : BoolResponseMessage
    {
        public AcceptGameResponse(bool success)
            : base(MessageType.AcceptGameResponse, success)
        {
        }

        public AcceptGameResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.AcceptGameResponse, messageNumber, connection, messageBody)
        {
        }
    }
    class StartingGameBroadcast : Message
    {
        public StartingGameBroadcast(int stage)
            : base(MessageType.StartingGameBroadcast)
        {
            this.stage = stage;
        }
        public StartingGameBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.StartingGameBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            stage = reader.ReadInt32();
        }

        private int stage;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(stage);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).StartingGameSequence(stage);
        }
    }

    abstract class ProfileMessage : Message
    {
        public ProfileMessage(MessageType messageType, Profile generateProfile)
            : base(messageType)
        {
            this.generateProfile = generateProfile;
        }
        public ProfileMessage(MessageType messageType, int messageNumber, Connection connection, byte[] messageBody)
            : base(messageType, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            generateProfile = new Profile();
            generateProfile.BoardStyle = (MeshType)reader.ReadInt32();
            generateProfile.BoardWidth = reader.ReadInt32();
            generateProfile.BoardHeight = reader.ReadInt32();
            generateProfile.GeneratorStyle = (SolverMethod)reader.ReadInt32();
            generateProfile.IterativeGeneratorDepth = reader.ReadInt32();
            generateProfile.GeneratorCellIntersInteract = reader.ReadBoolean();
            generateProfile.GenerateConsiderMultipleLoops = reader.ReadBoolean();
            generateProfile.LineToCrossRatio = reader.ReadDouble();
            generateProfile.ErrorRatio = reader.ReadDouble();
        }

        protected Profile generateProfile;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)generateProfile.BoardStyle);
            writer.Write(generateProfile.BoardWidth);
            writer.Write(generateProfile.BoardHeight);
            writer.Write((int)generateProfile.GeneratorStyle);
            writer.Write(generateProfile.IterativeGeneratorDepth);
            writer.Write(generateProfile.GeneratorCellIntersInteract);
            writer.Write(generateProfile.GenerateConsiderMultipleLoops);
            writer.Write(generateProfile.LineToCrossRatio);
            writer.Write(generateProfile.ErrorRatio);
            writer.Flush();
            return stream.ToArray();
        }
    }

    class GenerateMessage : ProfileMessage
    {
        public GenerateMessage(Profile generateProfile)
            : base(MessageType.GenerateRequest, generateProfile)
        {
        }
        public GenerateMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.GenerateRequest, messageNumber, connection, messageBody)
        {
        }

        public override void Process()
        {
            Thread processing = new Thread(PerformProcessing);
            processing.Start();
        }

        private void PerformProcessing()
        {
            Mesh mesh = new Mesh(generateProfile.BoardWidth, generateProfile.BoardHeight, generateProfile.BoardStyle);
            mesh.SolverMethod = generateProfile.GeneratorStyle;
            mesh.IterativeSolverDepth = generateProfile.IterativeGeneratorDepth;
            mesh.ConsiderMultipleLoops = generateProfile.GenerateConsiderMultipleLoops;
            mesh.UseIntersectCellInteractsInSolver = generateProfile.GeneratorCellIntersInteract;
            mesh.Generate();
            Mesh end = mesh.FinalSolution;
            Connection.PostResponse(new GenerateResponse(mesh, end), MessageNumber);
        }
    }

    class GenerateResponse : Message
    {
        public GenerateResponse(Mesh start, Mesh end)
            : base(MessageType.GenerateResponse)
        {
            StringWriter writer = new StringWriter();
            start.Save(writer);
            string startText = writer.ToString();
            writer.GetStringBuilder().Length = 0;
            end.Save(writer);
            string endText = writer.ToString();
            StartLines = startText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            EndLines = endText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        }

        public GenerateResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.GenerateResponse, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            MeshType = (MeshType)reader.ReadInt32();
            int startCount = reader.ReadInt32();
            StartLines = new string[startCount];
            for (int i = 0; i < startCount; i++)
            {
                StartLines[i] = reader.ReadString();
            }
            int endCount = reader.ReadInt32();
            EndLines = new string[endCount];
            for (int i = 0; i < endCount; i++)
            {
                EndLines[i] = reader.ReadString();
            }
        }

        public MeshType MeshType;

        public string[] StartLines;
        public string[] EndLines;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)MeshType);
            writer.Write(StartLines.Length);
            for (int i = 0; i < StartLines.Length; i++)
                writer.Write(StartLines[i]);
            writer.Write(EndLines.Length);
            for (int i = 0; i < EndLines.Length; i++)
                writer.Write(EndLines[i]);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            throw new InvalidOperationException("Responses should not be processed.");
        }

    }

    class BoardDetailsBroadcast : Message
    {
        public BoardDetailsBroadcast(Mesh start)
            : base(MessageType.BoardDetailsBroadcast)
        {
            StringWriter writer = new StringWriter();
            start.Save(writer);
            string startText = writer.ToString();
            StartLines = startText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        }

        public BoardDetailsBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.BoardDetailsBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            MeshType = (MeshType)reader.ReadInt32();
            int startCount = reader.ReadInt32();
            StartLines = new string[startCount];
            for (int i = 0; i < startCount; i++)
            {
                StartLines[i] = reader.ReadString();
            }
        }

        public MeshType MeshType;

        public string[] StartLines;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((int)MeshType);
            writer.Write(StartLines.Length);
            for (int i = 0; i < StartLines.Length; i++)
                writer.Write(StartLines[i]);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).InitialBoardDataReceived(StartLines, MeshType);
        }

    }

    class MoveMessage : Message
    {
       public MoveMessage(int edge, bool set)
            : base(MessageType.MakeMove)
        {
           this.edge = edge;
           this.set = set;
       }

        public MoveMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.MakeMove, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            edge = reader.ReadInt32();
            set = reader.ReadBoolean();
        }

        private int edge;
        private bool set;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(edge);
            writer.Write(set);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            bool success = ((Server.ServerGame)Connection.Player.Game).MakeMove(Connection.Player, edge, set);
            Connection.PostResponse(new MoveResponse(success), MessageNumber);
        }

    }

    class MoveResponse : BoolResponseMessage
    {
        public MoveResponse(bool success)
            : base(MessageType.MakeMoveResponse, success)
        {
        }

        public MoveResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.MakeMoveResponse, messageNumber, connection, messageBody)
        {
        }
    }



    class MoveBroadcast : Message
    {
        public MoveBroadcast(List<IAction> moves)
            : base(MessageType.MakeMoveBroadcast)
        {
            this.moves = new List<int[]>();
            foreach (SetAction action in moves)
            {
                this.moves.Add(new int[] { action.EdgeIndex, (int)action.EdgeState });
            }
        }

        public MoveBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.MakeMoveBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            int movesCount = reader.ReadInt32();
            this.moves = new List<int[]>();
            for (int i = 0; i < movesCount; i++)
            {
                int[] next = new int[2];
                next[0] = reader.ReadInt32();
                next[1] = reader.ReadInt32();
                moves.Add(next);
            }
        }

        List<int[]> moves;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(moves.Count);
            foreach (int[] move in moves)
            {
                writer.Write(move[0]);
                writer.Write(move[1]);
            }
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).MoveReceived(moves);
        }

    }


    class ScoresBroadcast : Message
    {
        public ScoresBroadcast(List<double> scores, bool playing)
            : base(MessageType.ScoresBroadcast)
        {
            this.scores = scores;
            this.playing = playing;
        }

        public ScoresBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ScoresBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            int scoresCount = reader.ReadInt32();
            this.scores = new List<double>();
            for (int i = 0; i < scoresCount; i++)
            {
                this.scores.Add(reader.ReadDouble());
            }
            this.playing = reader.ReadBoolean();
        }

        List<double> scores;
        bool playing;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(scores.Count);
            foreach (double score in scores)
            {
                writer.Write(score);
            }
            writer.Write(playing);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).ScoresReceived(scores, playing);
        }

    }

    class AcceptCountBroadcast : Message
    {
        public AcceptCountBroadcast(int count)
            : base(MessageType.AcceptCountBroadcast)
        {
            this.count = count;
        }

        public AcceptCountBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.AcceptCountBroadcast, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            this.count = reader.ReadInt32();
        }

        private int count;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(this.count);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).AcceptCountReceived(count);
        }

    }

    class HelloMessage : Message
    {
        public HelloMessage()
            : base(MessageType.Hello)
        {
            clientVersion = Connection.ProtocolVersion;
        }

        public HelloMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.Hello, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            this.clientVersion = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }

        Version clientVersion;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(clientVersion.Major);
            writer.Write(clientVersion.Minor);
            writer.Write(clientVersion.Build);
            writer.Write(clientVersion.Revision);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            if (clientVersion < Connection.ProtocolVersion || clientVersion.Major > Connection.ProtocolVersion.Major)
                Connection.PostResponse(new HelloResponse(), MessageNumber);
            else
            {
                Connection.Nonce = new byte[32];
                byte[] publickeydata = null;
                lock (((Server.ServerShard)Connection.Shard).CryptoLock)
                {
                    publickeydata = ((Server.ServerShard)Connection.Shard).RSA.ExportCspBlob(false);
                    ((Server.ServerShard)Connection.Shard).RNG.GetBytes(Connection.Nonce);
                }
                Connection.PostResponse(new HelloResponse(Connection.Nonce, publickeydata), MessageNumber);
            }
        }

    }

    class HelloResponse : Message
    {
        public HelloResponse()
            : base(MessageType.HelloResponse)
        {
            Success = false;
            Nonce = new byte[0];
            PublicKeyData = new byte[0];
        }
        public HelloResponse(byte[] nonce, byte[] publicKeyData)
            : base(MessageType.HelloResponse)
        {
            Success = true;
            Nonce = nonce;
            PublicKeyData = publicKeyData;
        }

        public HelloResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.HelloResponse, messageNumber, connection)
        {
            MemoryStream stream = new MemoryStream(messageBody);
            BinaryReader reader = new BinaryReader(stream);
            Success = reader.ReadBoolean();
            Nonce = reader.ReadBytes(reader.ReadInt32());
            PublicKeyData = reader.ReadBytes(reader.ReadInt32());
        }

        public bool Success;
        public byte[] Nonce;
        public byte[] PublicKeyData;

        public override byte[] GetBody()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Success);
            writer.Write(Nonce.Length);
            writer.Write(Nonce, 0, Nonce.Length);
            writer.Write(PublicKeyData.Length);
            writer.Write(PublicKeyData, 0, PublicKeyData.Length);
            writer.Flush();
            return stream.ToArray();
        }

        public override void Process()
        {
            throw new InvalidOperationException("Responses should never be processed.");
        }

    }

    class ProfileDetailsBroadcast : ProfileMessage
    {
        public ProfileDetailsBroadcast(Profile generateProfile)
            : base(MessageType.ProfileDetailsBroadcast, generateProfile)
        {
        }
        public ProfileDetailsBroadcast(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ProfileDetailsBroadcast, messageNumber, connection, messageBody)
        {
        }

        public override void Process()
        {
            ((Client.ClientShard)Connection.Shard).ProfileDetailsReceived(generateProfile);
        }
    }

    class ProfileDetailsMessage : ProfileMessage
    {
        public ProfileDetailsMessage(Profile generateProfile)
            : base(MessageType.ProfileDetails, generateProfile)
        {
        }
        public ProfileDetailsMessage(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ProfileDetails, messageNumber, connection, messageBody)
        {
        }

        public override void Process()
        {
            bool success = ((Server.ServerGame)Connection.Player.Game).UpdateProfile(Connection.Player, generateProfile);
            Connection.PostResponse(new ProfileDetailsResponse(success), MessageNumber);
        }
    }

    class ProfileDetailsResponse : BoolResponseMessage
    {
        public ProfileDetailsResponse(bool success)
            : base(MessageType.ProfileDetailsResponse, success)
        {
        }

        public ProfileDetailsResponse(int messageNumber, Connection connection, byte[] messageBody)
            : base(MessageType.ProfileDetailsResponse, messageNumber, connection, messageBody)
        {
        }
    }

}
