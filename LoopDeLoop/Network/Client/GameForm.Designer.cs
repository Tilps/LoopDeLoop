namespace LoopDeLoop.Network.Client
{
    partial class GameForm
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Players");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Spectators");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameForm));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.treePlayers = new System.Windows.Forms.TreeView();
            this.labelAccepted = new System.Windows.Forms.Label();
            this.buttonSettings = new System.Windows.Forms.Button();
            this.buttonAccept = new System.Windows.Forms.Button();
            this.textGameMessages = new System.Windows.Forms.TextBox();
            this.textGameChat = new System.Windows.Forms.TextBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.loopDisplay1 = new LoopDeLoop.LoopDisplay();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Size = new System.Drawing.Size(815, 528);
            this.splitContainer1.SplitterDistance = 271;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.labelStatus);
            this.splitContainer2.Panel2.Controls.Add(this.loopDisplay1);
            this.splitContainer2.Size = new System.Drawing.Size(815, 528);
            this.splitContainer2.SplitterDistance = 271;
            this.splitContainer2.TabIndex = 1;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.treePlayers);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.labelAccepted);
            this.splitContainer3.Panel2.Controls.Add(this.buttonSettings);
            this.splitContainer3.Panel2.Controls.Add(this.buttonAccept);
            this.splitContainer3.Panel2.Controls.Add(this.textGameMessages);
            this.splitContainer3.Panel2.Controls.Add(this.textGameChat);
            this.splitContainer3.Size = new System.Drawing.Size(271, 528);
            this.splitContainer3.SplitterDistance = 175;
            this.splitContainer3.TabIndex = 0;
            // 
            // treePlayers
            // 
            this.treePlayers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treePlayers.Location = new System.Drawing.Point(0, 0);
            this.treePlayers.Name = "treePlayers";
            treeNode1.Name = "Players";
            treeNode1.Text = "Players";
            treeNode2.Name = "Spectators";
            treeNode2.Text = "Spectators";
            this.treePlayers.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2});
            this.treePlayers.Size = new System.Drawing.Size(271, 175);
            this.treePlayers.TabIndex = 0;
            // 
            // labelAccepted
            // 
            this.labelAccepted.AutoSize = true;
            this.labelAccepted.Location = new System.Drawing.Point(12, 9);
            this.labelAccepted.Name = "labelAccepted";
            this.labelAccepted.Size = new System.Drawing.Size(0, 13);
            this.labelAccepted.TabIndex = 4;
            // 
            // buttonSettings
            // 
            this.buttonSettings.Location = new System.Drawing.Point(193, 4);
            this.buttonSettings.Name = "buttonSettings";
            this.buttonSettings.Size = new System.Drawing.Size(75, 23);
            this.buttonSettings.TabIndex = 3;
            this.buttonSettings.Text = "Settings";
            this.buttonSettings.UseVisualStyleBackColor = true;
            this.buttonSettings.Click += new System.EventHandler(this.buttonSettings_Click);
            // 
            // buttonAccept
            // 
            this.buttonAccept.Location = new System.Drawing.Point(112, 4);
            this.buttonAccept.Name = "buttonAccept";
            this.buttonAccept.Size = new System.Drawing.Size(75, 23);
            this.buttonAccept.TabIndex = 2;
            this.buttonAccept.Text = "Accept";
            this.buttonAccept.UseVisualStyleBackColor = true;
            this.buttonAccept.Click += new System.EventHandler(this.buttonAccept_Click);
            // 
            // textGameMessages
            // 
            this.textGameMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textGameMessages.Location = new System.Drawing.Point(3, 33);
            this.textGameMessages.Multiline = true;
            this.textGameMessages.Name = "textGameMessages";
            this.textGameMessages.ReadOnly = true;
            this.textGameMessages.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textGameMessages.Size = new System.Drawing.Size(265, 287);
            this.textGameMessages.TabIndex = 1;
            // 
            // textGameChat
            // 
            this.textGameChat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textGameChat.Location = new System.Drawing.Point(3, 326);
            this.textGameChat.Name = "textGameChat";
            this.textGameChat.Size = new System.Drawing.Size(265, 21);
            this.textGameChat.TabIndex = 0;
            this.textGameChat.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textGameChat_KeyUp);
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(3, 9);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(0, 13);
            this.labelStatus.TabIndex = 1;
            // 
            // loopDisplay1
            // 
            this.loopDisplay1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.loopDisplay1.AutoMove = 0;
            this.loopDisplay1.BackColor = System.Drawing.Color.FloralWhite;
            this.loopDisplay1.DisallowTriviallyFalse = false;
            this.loopDisplay1.Location = new System.Drawing.Point(3, 30);
            this.loopDisplay1.Margin = new System.Windows.Forms.Padding(12);
            this.loopDisplay1.Name = "loopDisplay1";
            this.loopDisplay1.NoToggle = false;
            this.loopDisplay1.ShowColors = false;
            this.loopDisplay1.Size = new System.Drawing.Size(534, 495);
            this.loopDisplay1.TabIndex = 0;
            this.loopDisplay1.Text = "loopDisplay1";
            this.loopDisplay1.MovePerformed += new LoopDeLoop.MoveEventHandler(this.loopDisplay1_MovePerformed);
            // 
            // GameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(815, 528);
            this.Controls.Add(this.splitContainer2);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GameForm";
            this.Text = "Multiplayer Loop-de-Loop";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GameForm_FormClosed);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.Panel2.PerformLayout();
            this.splitContainer3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.TreeView treePlayers;
        private System.Windows.Forms.TextBox textGameMessages;
        private System.Windows.Forms.TextBox textGameChat;
        private LoopDisplay loopDisplay1;
        private System.Windows.Forms.Button buttonSettings;
        private System.Windows.Forms.Button buttonAccept;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelAccepted;
    }
}