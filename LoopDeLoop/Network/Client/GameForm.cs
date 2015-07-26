using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using LoopDeLoop.Properties;

namespace LoopDeLoop.Network.Client
{
    internal partial class GameForm : Form
    {
        public GameForm()
        {
            InitializeComponent();
            loopDisplay1.NoToggle = true;
            loopDisplay1.Font = Settings.Default.BoardFont;
        }

        public ClientShard Shard
        {
            get
            {
                return shard;
            }
            set
            {
                shard = value;
                this.CreateHandle();
                shard.GameDetailsUpdated += new EventHandler(shard_GameDetailsUpdated);
                shard.GameSequenceBroadcast += new GameSequenceEventHandler(shard_GameSequenceBroadcast);
                shard.InitialBoardDataBroadcast += new BoardDataEventHandler(shard_InitialBoardDataBroadcast);
                shard.MoveBroadcast += new BoardMoveEventHandler(shard_MoveBroadcast);
                shard.ScoresBroadcast += new GameScoresEventHandler(shard_ScoresBroadcast);
                shard.ReceivedGameChatMessage += new LobbyChatEventHandler(shard_ReceivedGameChatMessage);
                shard.AcceptCountBroadcast += new AcceptCountEventHandler(shard_AcceptCountBroadcast);
                shard.ProfileDetailBroadcast += new ProfileDetailEventHandler(shard_ProfileDetailBroadcast);
            }
        }

        void shard_ProfileDetailBroadcast(object sender, ProfileDetailEventArgs args)
        {
            profile = args.Profile;
        }

        Profile profile;

        void shard_AcceptCountBroadcast(object sender, AcceptCountEventArgs args)
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new AcceptCountEventHandler(shard_AcceptCountBroadcast), sender, args);
                return;
            }
            bool all = false;
            lock (shard.CurrentLobbyLock)
            {
                lock (shard.CurrentLobby.Games)
                {
                    if (shard.CurrentLobby.Games.ContainsKey(OwnerName))
                    {
                        ClientGame game = (ClientGame)shard.CurrentLobby.Games[OwnerName];
                        lock (game.Players)
                        {
                            if (game.Players.Count == args.Count)
                                all = true;
                        }
                    }
                }
            }
            if (args.Count == 0)
                labelAccepted.Text = "None Accepted.";
            else if (!all)
                labelAccepted.Text = "Accepted by: " +args.Count.ToString();
            else
                labelAccepted.Text = "All Accept";
        }

        void shard_ReceivedGameChatMessage(object sender, LobbyChatEventArgs args)
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new LobbyChatEventHandler(shard_ReceivedGameChatMessage), sender, args);
                return;
            }
            string newLine;
            if (args.Sender == null)
            {
                newLine = "You: " + args.Message;
            }
            else
            {
                newLine = args.Sender + ": " + args.Message;
            }
            textGameMessages.Text = textGameMessages.Text + Environment.NewLine + (string)newLine;
            textGameMessages.SelectionStart = textGameMessages.Text.Length;
            textGameMessages.ScrollToCaret();

        }

        void shard_ScoresBroadcast(object sender, GameScoresEventArgs args)
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new GameScoresEventHandler(shard_ScoresBroadcast), sender, args);
                return;
            }
            string scoresString = args.Playing ? string.Empty : "GameOver: ";
            int maxIndex = -1;
            double maxScore = double.NegativeInfinity;
            for (int i = 0; i < args.Scores.Count; i++)
            {
                if (i > 0)
                    scoresString += ", ";
                scoresString += args.Scores[i].ToString();
                if (args.Scores[i] > maxScore)
                {
                    maxIndex = i;
                    maxScore = args.Scores[i];
                }
            }
            if (!args.Playing)
            {
                scoresString += " Player " + maxIndex.ToString() + " is the winner.";
            }
            labelStatus.Text = scoresString;
        }

        void shard_MoveBroadcast(object sender, BoardMoveEventArgs args)
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new BoardMoveEventHandler(shard_MoveBroadcast), sender, args);
                return;
            }
            List<IAction> backup = new List<IAction>();
            foreach (int[] move in args.Moves)
            {
                if (loopDisplay1.Mesh.Edges[move[0]].State != (EdgeState)move[1])
                {
                    if (loopDisplay1.Mesh.Edges[move[0]].State != EdgeState.Empty)
                    {
                        new UnsetAction(loopDisplay1.Mesh, move[0]).Perform();
                    }
                    loopDisplay1.Mesh.Perform(move[0], (EdgeState)move[1], backup, 0);
                }
            }
            loopDisplay1.Refresh();
        }

        void shard_InitialBoardDataBroadcast(object sender, BoardDataEventArgs args)
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new BoardDataEventHandler(shard_InitialBoardDataBroadcast), sender, args);
                return;
            }
            Mesh res = new Mesh(0, 0, args.MeshType);
            res.LoadFromText(args.Lines);
            loopDisplay1.Mesh = res;
            loopDisplay1.Refresh();
        }

        void shard_GameSequenceBroadcast(object sender, GameSequenceEventArgs args)
        {
            this.BeginInvoke(new ParameterizedThreadStart(UpdateStarting), (object)args.Stage);
        }

        private void UpdateStarting(object stageObj)
        {
            int stage = (int)stageObj;
            if (stage == -1)
                labelStatus.Text = "Generating";
            else
                labelStatus.Text = stage.ToString();
        }

        void shard_GameDetailsUpdated(object sender, EventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(UpdateGameDetails));
        }

        private void UpdateGameDetails()
        {
            lock (shard.CurrentLobbyLock)
            {
                lock (shard.CurrentLobby.Games)
                {
                    if (shard.CurrentLobby.Games.ContainsKey(OwnerName))
                    {
                        treePlayers.Nodes[0].Nodes.Clear();
                        treePlayers.Nodes[1].Nodes.Clear();
                        ClientGame game = (ClientGame)shard.CurrentLobby.Games[OwnerName];
                        lock (game.Players)
                        {
                            foreach (Player player in game.Players)
                            {
                                treePlayers.Nodes[0].Nodes.Add(player.Name);
                                if (!treePlayers.Nodes[0].IsExpanded)
                                    treePlayers.Nodes[0].Expand();
                            }
                        }
                        lock (game.Observers)
                        {
                            foreach (Player observer in game.Observers)
                            {
                                treePlayers.Nodes[1].Nodes.Add(observer.Name);
                            }
                        }
                    }
                }
            }
        }

        private ClientShard shard;

        public string OwnerName;

        private void GameForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            shard.GameDetailsUpdated -= new EventHandler(shard_GameDetailsUpdated);
            shard.GameSequenceBroadcast -= new GameSequenceEventHandler(shard_GameSequenceBroadcast);
            shard.InitialBoardDataBroadcast -= new BoardDataEventHandler(shard_InitialBoardDataBroadcast);
            shard.MoveBroadcast -= new BoardMoveEventHandler(shard_MoveBroadcast);
            shard.ScoresBroadcast -= new GameScoresEventHandler(shard_ScoresBroadcast);
            shard.ReceivedGameChatMessage -= new LobbyChatEventHandler(shard_ReceivedGameChatMessage);
            shard.ProfileDetailBroadcast -= new ProfileDetailEventHandler(shard_ProfileDetailBroadcast);
            try
            {
                shard.Me.Connection.SendMessage(new ExitGameMessage());
            }
            catch
            {
            }
        }

        private void buttonAccept_Click(object sender, EventArgs e)
        {
            shard.Me.Connection.SendMessage(new AcceptGameMessage());
        }

        private void loopDisplay1_MovePerformed(object sender, MoveEventArgs args)
        {
            Thread newThread = new Thread(SendMove);
            newThread.Start(new MoveMessage(args.Edge, args.Set));
        }

        private void SendMove(object data)
        {
            MoveMessage msg = (MoveMessage)data;
            shard.Me.Connection.SendMessage(msg);
        }

        private void textGameChat_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string toSend = textGameChat.Text;
                textGameChat.Text = string.Empty;
                if (shard != null && shard.Me != null && shard.Me.Connection != null && shard.CurrentLobby != null)
                {
                    shard.Me.Connection.PostBroadcast(new GameChatMessage(toSend));
                }
            }

        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            GameSettingsForm form = new GameSettingsForm();

            form.AllowMultipleLoops = !profile.GenerateConsiderMultipleLoops;
            form.SimpleSolver = profile.GeneratorStyle == SolverMethod.Iterative;
            form.SimpleSolverDepth = profile.IterativeGeneratorDepth;
            form.UseICinSolver = profile.GeneratorCellIntersInteract;
            form.MeshStyle = profile.BoardStyle;
            if (profile.BoardHeight == profile.BoardWidth)
                form.SizeText = profile.BoardWidth.ToString();
            else
                form.SizeText = profile.BoardWidth.ToString() + "x" + profile.BoardHeight.ToString();
            form.ReadOnly = shard.Me.Name != OwnerName;
            if (form.ShowDialog() == DialogResult.OK)
            {
                int width;
                int height;
                if (!LoopDeLoopForm.ParseSize(form.SizeText, form.MeshStyle, out width, out height))
                    return;
                profile.BoardStyle = form.MeshStyle;
                profile.BoardHeight = height;
                profile.BoardWidth = width;
                profile.GenerateConsiderMultipleLoops = !form.AllowMultipleLoops;
                profile.GeneratorCellIntersInteract = form.UseICinSolver;
                profile.GeneratorStyle = form.SimpleSolver ? SolverMethod.Iterative : SolverMethod.Recursive;
                profile.IterativeGeneratorDepth = form.SimpleSolverDepth;
                shard.Me.Connection.SendMessage(new ProfileDetailsMessage(profile));
            }
        }

    }
}