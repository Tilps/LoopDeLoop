using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LoopDeLoop.Network.Server
{
    internal partial class GameServerForm : Form
    {
        public GameServerForm()
        {
            InitializeComponent();
        }

        ServerShard shard;

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ServerTypeForm form = new ServerTypeForm();
            form.PortNumber = 1331;
            if (form.ShowDialog() == DialogResult.OK)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.RestoreDirectory = true;
                dialog.Filter = "Server Details (*.xml)|*.xml";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (shard != null)
                    {
                        shard.Stop();
                        shard.LogOccurred -= new LogEventHandler(shard_LogOccurred);
                    }
                    shard = new ServerShard();
                    shard.LogOccurred += new LogEventHandler(shard_LogOccurred);
                    shard.PortNumber = form.PortNumber;
                    shard.StorageFile = dialog.FileName;
                    shard.Start();
                }
            }
        }

        void shard_LogOccurred(object sender, LogEventArgs args)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new LogEventHandler(shard_LogOccurred), sender, args);
                return;
            }
            textBox1.Text = textBox1.Text + args.Message + Environment.NewLine + Environment.NewLine;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (shard != null)
                shard.AddLobby(new ServerLobby("/Timed/Hexagon3/Advanced", shard));
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.RestoreDirectory = true;
            dialog.Filter = "Server Details (*.xml)|*.xml";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (shard != null)
                {
                    shard.Stop();
                    shard.LogOccurred -= new LogEventHandler(shard_LogOccurred);
                }
                shard = new ServerShard();
                shard.LogOccurred += new LogEventHandler(shard_LogOccurred);
                shard.LoadFromSettings(dialog.FileName);
                shard.Start();
            }
        }
    }
}