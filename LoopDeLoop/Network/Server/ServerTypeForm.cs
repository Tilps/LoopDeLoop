using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LoopDeLoop.Network.Server
{
    internal partial class ServerTypeForm : Form
    {
        public ServerTypeForm()
        {
            InitializeComponent();
        }

        public int PortNumber
        {
            get
            {
                int port;
                if (int.TryParse(textPort.Text, out port))
                    return port;
                else
                    return 1331;
            }
            set
            {
                textPort.Text = value.ToString();
            }
        }
    }
}