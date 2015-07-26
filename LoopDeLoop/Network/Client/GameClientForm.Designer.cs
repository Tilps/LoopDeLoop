namespace LoopDeLoop.Network.Client
{
    partial class GameClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Home");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Games");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Players");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameClientForm));
            this.contextMenuNewGame = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.gameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeLobbies = new System.Windows.Forms.TreeView();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.treeLobbyContents = new System.Windows.Forms.TreeView();
            this.textLobbyMessages = new System.Windows.Forms.TextBox();
            this.textChatEntry = new System.Windows.Forms.TextBox();
            this.contextMenuExistingGame = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.joinGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.watchGameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuNewGame.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.contextMenuExistingGame.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuNewGame
            // 
            this.contextMenuNewGame.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newGameToolStripMenuItem});
            this.contextMenuNewGame.Name = "contextMenuNewGame";
            this.contextMenuNewGame.Size = new System.Drawing.Size(126, 26);
            this.contextMenuNewGame.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuNewGame_Opening);
            // 
            // newGameToolStripMenuItem
            // 
            this.newGameToolStripMenuItem.Name = "newGameToolStripMenuItem";
            this.newGameToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.newGameToolStripMenuItem.Text = "New Game";
            this.newGameToolStripMenuItem.Click += new System.EventHandler(this.newGameToolStripMenuItem_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gameToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(995, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // gameToolStripMenuItem
            // 
            this.gameToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectToolStripMenuItem,
            this.disconnectToolStripMenuItem});
            this.gameToolStripMenuItem.Name = "gameToolStripMenuItem";
            this.gameToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.gameToolStripMenuItem.Text = "Game";
            // 
            // connectToolStripMenuItem
            // 
            this.connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            this.connectToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.connectToolStripMenuItem.Text = "Connect";
            this.connectToolStripMenuItem.Click += new System.EventHandler(this.connectToolStripMenuItem_Click);
            // 
            // disconnectToolStripMenuItem
            // 
            this.disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            this.disconnectToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.disconnectToolStripMenuItem.Text = "Disconnect";
            this.disconnectToolStripMenuItem.Click += new System.EventHandler(this.disconnectToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeLobbies);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(995, 540);
            this.splitContainer1.SplitterDistance = 286;
            this.splitContainer1.TabIndex = 2;
            // 
            // treeLobbies
            // 
            this.treeLobbies.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeLobbies.Location = new System.Drawing.Point(0, 0);
            this.treeLobbies.Name = "treeLobbies";
            treeNode1.Name = "Node0";
            treeNode1.Text = "Home";
            this.treeLobbies.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1});
            this.treeLobbies.Size = new System.Drawing.Size(286, 540);
            this.treeLobbies.TabIndex = 0;
            this.treeLobbies.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeLobbies_AfterSelect);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.treeLobbyContents);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.textLobbyMessages);
            this.splitContainer2.Panel2.Controls.Add(this.textChatEntry);
            this.splitContainer2.Size = new System.Drawing.Size(705, 540);
            this.splitContainer2.SplitterDistance = 255;
            this.splitContainer2.TabIndex = 0;
            // 
            // treeLobbyContents
            // 
            this.treeLobbyContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeLobbyContents.Location = new System.Drawing.Point(0, 0);
            this.treeLobbyContents.Name = "treeLobbyContents";
            treeNode2.ContextMenuStrip = this.contextMenuNewGame;
            treeNode2.Name = "Node0";
            treeNode2.Text = "Games";
            treeNode3.Name = "Node1";
            treeNode3.Text = "Players";
            this.treeLobbyContents.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode2,
            treeNode3});
            this.treeLobbyContents.Size = new System.Drawing.Size(255, 540);
            this.treeLobbyContents.TabIndex = 0;
            this.treeLobbyContents.MouseDown += new System.Windows.Forms.MouseEventHandler(this.treeLobbyContents_MouseDown);
            // 
            // textLobbyMessages
            // 
            this.textLobbyMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textLobbyMessages.Location = new System.Drawing.Point(2, 3);
            this.textLobbyMessages.Multiline = true;
            this.textLobbyMessages.Name = "textLobbyMessages";
            this.textLobbyMessages.ReadOnly = true;
            this.textLobbyMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textLobbyMessages.Size = new System.Drawing.Size(441, 508);
            this.textLobbyMessages.TabIndex = 1;
            // 
            // textChatEntry
            // 
            this.textChatEntry.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textChatEntry.Location = new System.Drawing.Point(3, 517);
            this.textChatEntry.Name = "textChatEntry";
            this.textChatEntry.Size = new System.Drawing.Size(440, 21);
            this.textChatEntry.TabIndex = 0;
            this.textChatEntry.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textChatEntry_KeyUp);
            // 
            // contextMenuExistingGame
            // 
            this.contextMenuExistingGame.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.joinGameToolStripMenuItem,
            this.watchGameToolStripMenuItem});
            this.contextMenuExistingGame.Name = "contextMenuExistingGame";
            this.contextMenuExistingGame.Size = new System.Drawing.Size(136, 48);
            // 
            // joinGameToolStripMenuItem
            // 
            this.joinGameToolStripMenuItem.Name = "joinGameToolStripMenuItem";
            this.joinGameToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.joinGameToolStripMenuItem.Text = "Join Game";
            this.joinGameToolStripMenuItem.Click += new System.EventHandler(this.joinGameToolStripMenuItem_Click);
            // 
            // watchGameToolStripMenuItem
            // 
            this.watchGameToolStripMenuItem.Name = "watchGameToolStripMenuItem";
            this.watchGameToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.watchGameToolStripMenuItem.Text = "Watch Game";
            this.watchGameToolStripMenuItem.Click += new System.EventHandler(this.watchGameToolStripMenuItem_Click);
            // 
            // GameClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(995, 564);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "GameClientForm";
            this.Text = "Loop-de-Loop World";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GameClientForm_FormClosed);
            this.contextMenuNewGame.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.contextMenuExistingGame.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem gameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disconnectToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeLobbies;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView treeLobbyContents;
        private System.Windows.Forms.TextBox textLobbyMessages;
        private System.Windows.Forms.TextBox textChatEntry;
        private System.Windows.Forms.ContextMenuStrip contextMenuNewGame;
        private System.Windows.Forms.ToolStripMenuItem newGameToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuExistingGame;
        private System.Windows.Forms.ToolStripMenuItem joinGameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem watchGameToolStripMenuItem;
    }
}