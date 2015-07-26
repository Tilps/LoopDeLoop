using System;
using System.Collections.Generic;
using System.Text;

namespace LoopDeLoop.Network.Client
{
    class ClientShard : Shard
    {
        public Player Me;

        public object CurrentLobbyLock = new object();

        public ClientLobby CurrentLobby;

        internal void AddLobbies(List<string> strings)
        {
            lock (Lobbies)
            {
                foreach (string str in strings)
                {
                    if (!Lobbies.ContainsKey(str))
                        Lobbies.Add(str, new ClientLobby(str, this));
                }
                lock (CurrentLobbyLock)
                {
                    if (CurrentLobby == null)
                    {
                        CurrentLobby = (ClientLobby)Lobbies["/"];
                    }
                }
            }
            if (LobbiesAdded != null)
                LobbiesAdded(this, EventArgs.Empty);
        }

        public event EventHandler LobbiesAdded;

        public event EventHandler PlayersAddedToCurrentLobby;
        public event EventHandler PlayersRemovedFromCurrentLobby;
        public event EventHandler GamesAddedToCurrentLobby;
        public event EventHandler GamesRemovedFromCurrentLobby;

        internal void AddPlayerToLobby(string playerName, string lobbyName)
        {
            lock (CurrentLobbyLock)
            {
                // Dodgy, but lets ignore messages for the wrong lobby for now.
                if (CurrentLobby == null || CurrentLobby.Name != lobbyName)
                    return;
                CurrentLobby.AddPlayer(playerName);
            }
            if (PlayersAddedToCurrentLobby != null)
                PlayersAddedToCurrentLobby(this, EventArgs.Empty);
        }

        internal void AddPlayersToLobby(List<string> playerNames, string lobbyName)
        {
            lock (Lobbies)
            {
                lock (CurrentLobbyLock)
                {
                    if (CurrentLobby == null || CurrentLobby.Name != lobbyName)
                        CurrentLobby = (ClientLobby)Lobbies[lobbyName];
                    CurrentLobby.ClearPlayers();
                    foreach (string playerName in playerNames)
                        CurrentLobby.AddPlayer(playerName);
                }
            }
            if (PlayersAddedToCurrentLobby != null)
                PlayersAddedToCurrentLobby(this, EventArgs.Empty);
        }

        public event LobbyChatEventHandler ReceivedLobbyChatMessage;

        internal void ReceiveLobbyChat(string message, string sender)
        {
            if (sender == Me.Name)
                sender = null;
            if (ReceivedLobbyChatMessage != null)
                ReceivedLobbyChatMessage(this, new LobbyChatEventArgs(message, sender));
        }

        internal void RemovePlayerFromLobby(string playerName, string lobbyName)
        {
            lock (CurrentLobbyLock)
            {
                // Dodgy, but lets ignore messages for the wrong lobby for now.
                if (CurrentLobby == null || CurrentLobby.Name != lobbyName)
                    return;
                CurrentLobby.RemovePlayer(playerName);
            }
            if (PlayersRemovedFromCurrentLobby != null)
                PlayersRemovedFromCurrentLobby(this, EventArgs.Empty);
        }

        internal void AddGamesToLobby(List<string> gameOwnerNames, string lobbyName)
        {
            lock (Lobbies)
            {
                lock (CurrentLobbyLock)
                {
                    if (CurrentLobby == null || CurrentLobby.Name != lobbyName)
                        CurrentLobby = (ClientLobby)Lobbies[lobbyName];
                    CurrentLobby.ClearGames();
                    foreach (string gameOwnerName in gameOwnerNames)
                        CurrentLobby.AddGame(gameOwnerName);
                }
            }
            if (GamesAddedToCurrentLobby != null)
                GamesAddedToCurrentLobby(this, EventArgs.Empty);
        }
        internal void AddGameToLobby(string gameOwnerName, string lobbyName)
        {
            lock (CurrentLobbyLock)
            {
                // Dodgy, but lets ignore messages for the wrong lobby for now.
                if (CurrentLobby == null || CurrentLobby.Name != lobbyName)
                    return;
                CurrentLobby.AddGame(gameOwnerName);
            }
            if (GamesAddedToCurrentLobby != null)
                GamesAddedToCurrentLobby(this, EventArgs.Empty);
        }
        internal void RemoveGameFromLobby(string gameOwnerName, string lobbyName)
        {
            lock (CurrentLobbyLock)
            {
                // Dodgy, but lets ignore messages for the wrong lobby for now.
                if (CurrentLobby == null || CurrentLobby.Name != lobbyName)
                    return;
                CurrentLobby.RemoveGame(gameOwnerName);
            }
            if (GamesRemovedFromCurrentLobby != null)
                GamesRemovedFromCurrentLobby(this, EventArgs.Empty);
        }

        internal void UpdateGameDetails(List<string> players, List<string> observers, string gameOwner)
        {
            lock (CurrentLobbyLock)
            {
                if (CurrentLobby == null)
                    return;
                CurrentLobby.UpdateGameDetails(players, observers, gameOwner);
            }
            if (GameDetailsUpdated != null)
                GameDetailsUpdated(this, EventArgs.Empty);
        }

        public event EventHandler GameDetailsUpdated;

        internal void PlayerJoinedGame(string playerName, string gameOwner, bool playing)
        {
            lock (CurrentLobbyLock)
            {
                if (CurrentLobby == null)
                    return;
                CurrentLobby.PlayerJoinedGame(playerName, gameOwner, playing);
            }
            if (GameDetailsUpdated != null)
                GameDetailsUpdated(this, EventArgs.Empty);
        }

        internal void StartingGameSequence(int stage)
        {
            if (GameSequenceBroadcast != null)
                GameSequenceBroadcast(this, new GameSequenceEventArgs(stage));
        }
        public event GameSequenceEventHandler GameSequenceBroadcast;

        internal void InitialBoardDataReceived(string[] startLines, MeshType meshType)
        {
            if (InitialBoardDataBroadcast != null)
                InitialBoardDataBroadcast(this, new BoardDataEventArgs(startLines, meshType));
        }
        public event BoardDataEventHandler InitialBoardDataBroadcast;

        internal void MoveReceived(List<int[]> moves)
        {
            if (MoveBroadcast != null)
                MoveBroadcast(this, new BoardMoveEventArgs(moves));
        }

        public event BoardMoveEventHandler MoveBroadcast;


        internal void ScoresReceived(List<double> scores, bool playing)
        {
            if (ScoresBroadcast != null)
                ScoresBroadcast(this, new GameScoresEventArgs(scores, playing));
        }

        public event GameScoresEventHandler ScoresBroadcast;

        internal void PlayerLeftGame(string playerName, string gameOwner)
        {
            lock (CurrentLobbyLock)
            {
                if (CurrentLobby == null)
                    return;
                CurrentLobby.PlayerExitedGame(playerName, gameOwner);
            }
            if (GameDetailsUpdated != null)
                GameDetailsUpdated(this, EventArgs.Empty);
        }
        public event LobbyChatEventHandler ReceivedGameChatMessage;

        internal void ReceiveGameChat(string message, string sender)
        {
            if (sender == Me.Name)
                sender = null;
            if (ReceivedGameChatMessage != null)
                ReceivedGameChatMessage(this, new LobbyChatEventArgs(message, sender));
        }

        public event AcceptCountEventHandler AcceptCountBroadcast;

        internal void AcceptCountReceived(int count)
        {
            if (AcceptCountBroadcast != null)
                AcceptCountBroadcast(this, new AcceptCountEventArgs(count));
        }

        internal void ProfileDetailsReceived(Profile generateProfile)
        {
            if (ProfileDetailBroadcast != null)
                ProfileDetailBroadcast(this, new ProfileDetailEventArgs(generateProfile));
        }

        public event ProfileDetailEventHandler ProfileDetailBroadcast;
    }

    delegate void ProfileDetailEventHandler(object sender, ProfileDetailEventArgs args);

    class ProfileDetailEventArgs : EventArgs
    {
        public ProfileDetailEventArgs(Profile profile)
        {
            this.Profile = profile;
        }

        public Profile Profile;
    }

    delegate void GameSequenceEventHandler(object sender, GameSequenceEventArgs args);

    class GameSequenceEventArgs : EventArgs
    {
        public GameSequenceEventArgs(int stage)
        {
            this.Stage = stage;
        }
        public int Stage;
    }
    delegate void AcceptCountEventHandler(object sender, AcceptCountEventArgs args);

    class AcceptCountEventArgs : EventArgs
    {
        public AcceptCountEventArgs(int count)
        {
            this.Count = count;
        }
        public int Count;
    }

    delegate void BoardDataEventHandler(object sender, BoardDataEventArgs args);

    class BoardDataEventArgs : EventArgs
    {
        public BoardDataEventArgs(string[] lines, MeshType meshType)
        {
            this.Lines = lines;
            this.MeshType = meshType;
        }
        public string[] Lines;

        public MeshType MeshType;
    }
    delegate void BoardMoveEventHandler(object sender, BoardMoveEventArgs args);

    class BoardMoveEventArgs : EventArgs
    {
        public BoardMoveEventArgs(List<int[]> moves)
        {
            this.Moves = moves;
        }
        public List<int[]> Moves;
    }
    delegate void GameScoresEventHandler(object sender, GameScoresEventArgs args);

    class GameScoresEventArgs : EventArgs
    {
        public GameScoresEventArgs(List<double> scores, bool playing)
        {
            this.Scores = scores;
            this.Playing = playing;
        }
        public List<double> Scores;

        public bool Playing;
    }


    delegate void LobbyChatEventHandler(object sender, LobbyChatEventArgs args);

    class LobbyChatEventArgs : EventArgs
    {
        public LobbyChatEventArgs(string message, string sender)
        {
            this.Message = message;
            this.Sender = sender;
        }
        public string Message;
        public string Sender;
    }
}
