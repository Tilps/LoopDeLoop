using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LoopDeLoop.Network.Server
{
    class ServerGame : Game
    {
        public ServerGame(string ownerName)
            : base(ownerName)
        {
            Profile = new Profile();
        }

        public List<bool> Accepts = new List<bool>();

        public void Broadcast(Message msg)
        {
            Player[] players;
            lock (Players)
            {
                players = new Player[Players.Count];
                Players.CopyTo(players, 0);
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
            lock (Observers)
            {
                players = new Player[Observers.Count];
                Observers.CopyTo(players);
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
                if (!Players.Contains(player))
                {
                    Players.Add(player);
                    Accepts.Add(false);
                }
                player.Game = this;
                foreach (Player existingPlayer in Players)
                    players.Add(existingPlayer.Name);
            }
            List<string> observers = new List<string>();
            lock (Observers)
            {
                foreach (Player existingObserver in Observers)
                {
                    observers.Add(existingObserver.Name);
                }
            }
            player.Connection.PostBroadcast(new PlayerExistsInGameBroadcast(players, observers, OwnerName));
            if (current != null)
                player.Connection.PostBroadcast(new BoardDetailsBroadcast(current));
            player.Connection.PostBroadcast(new ProfileDetailsBroadcast(Profile));
            Broadcast(new PlayerEnteredGameBroadcast(player.Name, OwnerName, true));
        }

        internal void AddObserver(Player player)
        {
            List<string> players = new List<string>();
            lock (Players)
            {
                foreach (Player existingPlayer in Players)
                    players.Add(existingPlayer.Name);
            }
            List<string> observers = new List<string>();
            lock (Observers)
            {
                if (!Observers.Contains(player))
                    Observers.Add(player);
                player.Game = this;
                foreach (Player existingObserver in Observers)
                {
                    observers.Add(existingObserver.Name);
                }
            }
            player.Connection.PostBroadcast(new PlayerExistsInGameBroadcast(players, observers, OwnerName));
            if (current != null)
                player.Connection.PostBroadcast(new BoardDetailsBroadcast(current));
            player.Connection.PostBroadcast(new ProfileDetailsBroadcast(Profile));
            Broadcast(new PlayerEnteredGameBroadcast(player.Name, OwnerName, false));
        }

        internal void AcceptGame(Player player)
        {
            lock (gameLock)
            {
                if (playingAllowed)
                    return;
            }
            lock (Players) {

                int index = Players.IndexOf(player);
                if (index != -1)
                    Accepts[index] = true;
                bool allAccept = true;
                int count = 0;
                foreach (bool accept in Accepts)
                    if (!accept)
                        allAccept = false;
                    else
                        count++;
                if (allAccept)
                {
                    Thread startGame = new Thread(StartGame);
                    startGame.Start();
                }
                Broadcast(new AcceptCountBroadcast(count));
            }
        }

        private object gameLock = new object();
        private Mesh current;
        private Mesh target;
        private List<double> scores = new List<double>();

        private bool playingAllowed = false;
        private void StartGame()
        {
            Random rnd = new Random();
            lock (Players)
            {
                Generator = Players[rnd.Next(Players.Count)];
            }
            Broadcast(new StartingGameBroadcast(-1));
            GenerateResponse res = (GenerateResponse)Generator.Connection.SendMessage(new GenerateMessage(Profile));
            current = new Mesh(0, 0, res.MeshType);
            target = new Mesh(0, 0, res.MeshType);
            current.LoadFromText(res.StartLines);
            target.LoadFromText(res.EndLines);
            for (int i = 5; i >= 0; i--)
            {
                Thread.Sleep(1000);
                Broadcast(new StartingGameBroadcast(i));
            }
            scores.Clear();
            for (int i = 0; i < Players.Count; i++)
                scores.Add(0.0);
            Broadcast(new ScoresBroadcast(scores, true));
            Broadcast(new BoardDetailsBroadcast(current));
            playingAllowed = true;
        }

        internal bool MakeMove(Player player, int edge, bool set)
        {
            List<IAction> backup = new List<IAction>();
            lock (gameLock)
            {
                if (!playingAllowed)
                    return false;
                if (current.Edges[edge].State != EdgeState.Empty)
                    return false;
                int index;
                lock (Players)
                {
                    index = Players.IndexOf(player);
                }
                if (index == -1)
                    return false;
                double points;
                if (target.Edges[edge].State == EdgeState.Filled)
                    points = set ? Profile.LineToCrossRatio : -Profile.ErrorRatio;
                else
                    points = set ? -Profile.LineToCrossRatio*Profile.ErrorRatio : 1;
                current.Perform(edge, target.Edges[edge].State, backup, 0);
                scores[index] += points;
                bool done = true;
                for (int i = 0; i < current.Edges.Count; i++)
                {
                    if (current.Edges[i].State == EdgeState.Empty)
                        done = false;
                }
                if (done)
                {
                    // TODO: update scores if rating match, Send Winner message.
                    playingAllowed = false;
                    lock (Players)
                    {
                        for (int i = 0; i < Accepts.Count; i++)
                            Accepts[i] = false;
                    }
                    Broadcast(new AcceptCountBroadcast(0));
                }
                Broadcast(new MoveBroadcast(backup));
                Broadcast(new ScoresBroadcast(scores, playingAllowed));
            }
            return true;
        }

        internal void RemovePlayer(Player player)
        {
            int i = -1;
            bool anyLeft = false;
            lock (Players)
            {
                if (Players.Contains(player))
                {
                    i = Players.IndexOf(player);
                    Players.RemoveAt(i);
                    Accepts.RemoveAt(i);

                }
                if (Players.Count != 0)
                    anyLeft = true;
                player.Game = null;
            }
            if (i > 0)
            {
                lock (gameLock)
                {
                    if (scores.Count > i)
                        scores.RemoveAt(i);
                }
                // TODO: send forfeit message if score removed.
            }
            lock (Observers)
            {
                if (Observers.Contains(player))
                    Observers.Remove(player);
                player.Game = null;
                if (Observers.Count != 0)
                    anyLeft = true;
            }
            Broadcast(new PlayerExitedGameBroadcast(player.Name, OwnerName));
            if (!anyLeft)
                ((ServerLobby)Lobby).RemoveGame(this.OwnerName);
        }

        internal bool UpdateProfile(Player player, Profile generateProfile)
        {
            lock (Players)
            {
                if (player.Name != OwnerName)
                    return false;
                if (playingAllowed)
                    return false;
                Profile = generateProfile;
            }
            Broadcast(new ProfileDetailsBroadcast(generateProfile));
            return true;
        }
    }
}
