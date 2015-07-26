using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace LoopDeLoop.Network.Client
{
    public partial class GameClientForm : Form
    {
        public GameClientForm()
        {
            InitializeComponent();
        }

        ClientShard shard;

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shard = new ClientShard();
            shard.LobbiesAdded += new EventHandler(shard_LobbiesAdded);
            shard.PlayersAddedToCurrentLobby += new EventHandler(shard_PlayersAddedToCurrentLobby);
            shard.PlayersRemovedFromCurrentLobby += new EventHandler(shard_PlayersRemovedFromCurrentLobby);
            shard.GamesAddedToCurrentLobby += new EventHandler(shard_GamesAddedToCurrentLobby);
            shard.GamesRemovedFromCurrentLobby += new EventHandler(shard_GamesRemovedFromCurrentLobby);
            shard.ReceivedLobbyChatMessage += new LobbyChatEventHandler(shard_ReceivedLobbyChatMessage);
            Player player = new Player("");
            ConnectForm form = new ConnectForm();
            form.Player = player;
            form.Port = 1331;
            form.Hostname = "deadofnight.org";
            form.ParentShard = shard;
            if (form.ShowDialog() == DialogResult.OK)
            {
                shard.Me = player;
            }
            else
            {
                shard = null;
            }
        }

        void shard_GamesRemovedFromCurrentLobby(object sender, EventArgs e)
        {
            string[] games;
            lock (shard.CurrentLobbyLock)
            {
                lock (shard.CurrentLobby.Games)
                {
                    games = new string[shard.CurrentLobby.Games.Count];
                    shard.CurrentLobby.Games.Keys.CopyTo(games, 0);
                }
            }
            this.BeginInvoke(new ParameterizedThreadStart(CheckMissingLobbyGames), (object)games);
        }

        void shard_GamesAddedToCurrentLobby(object sender, EventArgs e)
        {
            string[] games;
            lock (shard.CurrentLobbyLock)
            {
                lock (shard.CurrentLobby.Games)
                {
                    games = new string[shard.CurrentLobby.Games.Count];
                    shard.CurrentLobby.Games.Keys.CopyTo(games, 0);
                }
            }
            this.BeginInvoke(new ParameterizedThreadStart(EnsureLobbyGames), (object)games);
        }

        void shard_ReceivedLobbyChatMessage(object sender, LobbyChatEventArgs args)
        {
            string newLine;
            if (args.Sender == null)
            {
                newLine ="You: " + args.Message;
            }
            else
            {
                newLine = args.Sender + ": " + args.Message;
            }
            this.BeginInvoke(new ParameterizedThreadStart(AddMessage), (object)newLine);
        }

        void AddMessage(object message)
        {
            textLobbyMessages.Text = textLobbyMessages.Text + Environment.NewLine + (string)message;
            textLobbyMessages.SelectionStart = textLobbyMessages.Text.Length;
            textLobbyMessages.ScrollToCaret();
        }

        void shard_PlayersRemovedFromCurrentLobby(object sender, EventArgs e)
        {
            string[] players;
            lock (shard.CurrentLobbyLock)
            {
                lock (shard.CurrentLobby.Players)
                {
                    players = new string[shard.CurrentLobby.Players.Count];
                    shard.CurrentLobby.Players.Keys.CopyTo(players, 0);
                }
            }
            this.BeginInvoke(new ParameterizedThreadStart(CheckMissingLobbyPlayers), (object)players);
        }

        void shard_PlayersAddedToCurrentLobby(object sender, EventArgs e)
        {
            string[] players;
            lock (shard.CurrentLobbyLock)
            {
                lock (shard.CurrentLobby.Players)
                {
                    players = new string[shard.CurrentLobby.Players.Count];
                    shard.CurrentLobby.Players.Keys.CopyTo(players, 0);
                }
            }
            this.BeginInvoke(new ParameterizedThreadStart(EnsureLobbyPlayers), (object)players);
        }

        string currentLobbyName;

        void EnsureLobbyPlayers(object playerList)
        {
            string[] players = (string[])playerList;
            bool clearing = false;
            lock (shard.CurrentLobbyLock)
            {
                if (shard.CurrentLobby.Name != currentLobbyName)
                {
                    currentLobbyName = shard.CurrentLobby.Name;
                    treeLobbyContents.Nodes[1].Nodes.Clear();
                    clearing = true;
                }
            }
            if (clearing)
                textLobbyMessages.Text = string.Empty;
            foreach (string player in players)
            {
                int i;
                if ((i = treeLobbyContents.Nodes[1].Nodes.IndexOfKey(player)) == -1)
                {
                    treeLobbyContents.Nodes[1].Nodes.Add(player, player);
                    if (!treeLobbyContents.Nodes[1].IsExpanded)
                        treeLobbyContents.Nodes[1].Expand();
                    if (!clearing)
                        AddMessage("System: " + player + " has arrived.");
                }
            }
        }
        void CheckMissingLobbyPlayers(object playerList)
        {
            string[] players = (string[])playerList;
            List<TreeNode> toRemove = new List<TreeNode>();
            foreach (TreeNode node in treeLobbyContents.Nodes[1].Nodes)
            {
                if (Array.IndexOf(players, node.Name) == -1)
                {
                    toRemove.Add(node);
                    if (node.Name != shard.Me.Name)
                        AddMessage("System: " + node.Name + " has left.");
                }
            }
            foreach (TreeNode toRemoveNode in toRemove)
            {
                treeLobbyContents.Nodes[1].Nodes.Remove(toRemoveNode);
            }
        }
        string currentLobbyName2;
        void EnsureLobbyGames(object gamesList)
        {
            string[] games = (string[])gamesList;
            bool clearing = false;
            lock (shard.CurrentLobbyLock)
            {
                if (shard.CurrentLobby.Name != currentLobbyName2)
                {
                    currentLobbyName2 = shard.CurrentLobby.Name;
                    treeLobbyContents.Nodes[0].Nodes.Clear();
                    clearing = true;
                }
            }
            if (clearing)
                textLobbyMessages.Text = string.Empty;
            foreach (string game in games)
            {
                int i;
                if ((i = treeLobbyContents.Nodes[0].Nodes.IndexOfKey(game)) == -1)
                {
                    TreeNode newNode = treeLobbyContents.Nodes[0].Nodes.Add(game, game);
                    if (!treeLobbyContents.Nodes[0].IsExpanded)
                        treeLobbyContents.Nodes[0].Expand();
                    newNode.ContextMenuStrip = contextMenuExistingGame;
                    if (!clearing)
                        AddMessage("System: " + game + " has started a game.");
                }
            }
        }
        void CheckMissingLobbyGames(object gamesList)
        {
            string[] games = (string[])gamesList;
            List<TreeNode> toRemove = new List<TreeNode>();
            foreach (TreeNode node in treeLobbyContents.Nodes[0].Nodes)
            {
                if (Array.IndexOf(games, node.Name) == -1)
                {
                    toRemove.Add(node);
                    AddMessage("System: The game started by " + node.Name + " has finished.");
                }
            }
            foreach (TreeNode toRemoveNode in toRemove)
            {
                treeLobbyContents.Nodes[0].Nodes.Remove(toRemoveNode);
            }
        }

        void shard_LobbiesAdded(object sender, EventArgs e)
        {
            string[] lobbies;
            lock (shard.Lobbies)
            {
                lobbies = new string[shard.Lobbies.Count];
                shard.Lobbies.Keys.CopyTo(lobbies, 0);
            }
            this.BeginInvoke(new ParameterizedThreadStart(EnsureLobbies), (object)lobbies);
        }

        void EnsureLobbies(object lobbiesList)
        {
            string[] lobbies = (string[])lobbiesList;
            foreach (string lobby in lobbies)
            {
                string[] pathBits = lobby.TrimEnd('/').Split('/');
                TreeNode cur = treeLobbies.Nodes[0];
                for (int i = 1; i < pathBits.Length; i++)
                {
                    string bit = pathBits[i];
                    if (cur.Nodes.ContainsKey(bit))
                    {
                        cur = cur.Nodes[cur.Nodes.IndexOfKey(bit)];
                    }
                    else
                    {
                        cur = cur.Nodes.Add(bit, bit);
                    }
                }
            }
            treeLobbies.Refresh();
        }
 
        private void textChatEntry_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string toSend = textChatEntry.Text;
                textChatEntry.Text = string.Empty;
                if (shard != null && shard.Me != null && shard.Me.Connection != null && shard.CurrentLobby != null)
                {
                    shard.Me.Connection.PostBroadcast(new LobbyChatMessage(toSend));
                }
            }
        }

        private void treeLobbies_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (shard != null)
            {
                string lobbyName = FormLobbyName(e.Node);
                lock (shard.Lobbies)
                {
                    if (shard.Lobbies.ContainsKey(lobbyName))
                    {
                        ClientLobby lobby = (ClientLobby)shard.Lobbies[lobbyName];
                        Thread changeLobbyThread = new Thread(ChangeLobby);
                        changeLobbyThread.Start(lobby.Name);

                    }
                }
            }
        }

        private string FormLobbyName(TreeNode node)
        {
            StringBuilder builder = new StringBuilder();
            while (node.Parent != null)
            {
                builder.Insert(0, node.Name + "/");
                node = node.Parent;
            }
            builder.Insert(0, "/");
            string res = builder.ToString();
            if (res.Length > 1)
                res = res.TrimEnd('/');
            return res;
        }

        private void ChangeLobby(object newLobby)
        {
            string lobbyName = (string)newLobby;
            shard.Me.Connection.SendMessage(new ChangeLobbyMessage(lobbyName));
        }

        private void contextMenuNewGame_Opening(object sender, CancelEventArgs e)
        {
            contextMenuNewGame.Items[0].Enabled = shard != null && shard.Me != null && shard.Me.Connection != null;
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameForm form = new GameForm();
            form.OwnerName = this.shard.Me.Name;
            form.Shard = this.shard;
            NewGameResponse response = (NewGameResponse)shard.Me.Connection.SendMessage(new NewGameMessage());
            if (response.Success)
            {
                form.ShowDialog();
            }
            else
            {
                MessageBox.Show("Unable to create new game.");
            }
        }
        TreeNode lastClickedNode = null;
        private void joinGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lastClickedNode == null)
                return;
            GameForm form = new GameForm();
            form.OwnerName = lastClickedNode.Name;
            form.Shard = this.shard;
            JoinGameResponse response = (JoinGameResponse)shard.Me.Connection.SendMessage(new JoinGameMessage(lastClickedNode.Name, true));
            if (response.Success)
            {
                form.ShowDialog();
            }
            else
            {
                MessageBox.Show("Game is locked.");
            }

        }

        private void watchGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lastClickedNode == null)
                return;
            GameForm form = new GameForm();
            form.OwnerName = lastClickedNode.Name;
            form.Shard = this.shard;
            JoinGameResponse response = (JoinGameResponse)shard.Me.Connection.SendMessage(new JoinGameMessage(lastClickedNode.Name, false));
            if (response.Success)
            {
                form.ShowDialog();
            }
            else
            {
                MessageBox.Show("Game is locked.");
            }


        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (shard != null && shard.Me != null && shard.Me.Connection != null)
            {
                shard.Me.Connection.Stop();
                shard.LobbiesAdded -= new EventHandler(shard_LobbiesAdded);
                shard.PlayersAddedToCurrentLobby -= new EventHandler(shard_PlayersAddedToCurrentLobby);
                shard.PlayersRemovedFromCurrentLobby -= new EventHandler(shard_PlayersRemovedFromCurrentLobby);
                shard.GamesAddedToCurrentLobby -= new EventHandler(shard_GamesAddedToCurrentLobby);
                shard.GamesRemovedFromCurrentLobby -= new EventHandler(shard_GamesRemovedFromCurrentLobby);
                shard.ReceivedLobbyChatMessage -= new LobbyChatEventHandler(shard_ReceivedLobbyChatMessage);
                shard = null;
                treeLobbies.Nodes[0].Nodes.Clear();
                treeLobbyContents.Nodes[0].Nodes.Clear();
                treeLobbyContents.Nodes[1].Nodes.Clear();
                textLobbyMessages.Text = string.Empty;
            }
        }

        private void treeLobbyContents_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode point = treeLobbyContents.GetNodeAt(e.Location);
            if (point != null)
                lastClickedNode = point;
        }

        private void GameClientForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            disconnectToolStripMenuItem_Click(sender, e);
        }
    }
}