using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;

namespace LoopDeLoop.Network.Client
{
    internal partial class ConnectForm : Form
    {
        public ConnectForm()
        {
            InitializeComponent();
        }

        public ClientShard ParentShard;

        public string Hostname
        {
            get
            {
                return textServer.Text;
            }
            set
            {
                textServer.Text = value;
            }
        }

        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
                textPort.Text = port.ToString();
            }
        }
        private int port;

        public Player Player
        {
            get
            {
                return player;
            }
            set
            {
                player = value;
                textPassword.Text = string.Empty;
                textUsername.Text = player.Name;
            }
        }
        private Player player;

        private void button1_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textPort.Text, out port))
            {
                MessageBox.Show("Please enter a number in the port section.");
                DialogResult = DialogResult.None;
                return;
            }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(Hostname, port);
            }
            catch
            {
                MessageBox.Show("Unable to connect to chosen server on specified port.");
                DialogResult = DialogResult.None;
                return;
            }
            try
            {
                Connection con = new Connection(socket, ParentShard);
                player.Name = textUsername.Text;
                SHA256 hasher = SHA256.Create();
                player.PasswordHash = hasher.ComputeHash(Encoding.Unicode.GetBytes(textPassword.Text));
                player.Connection = con;
                con.Player = player;
                con.Start();
                HelloResponse res = (HelloResponse)con.SendMessage(new HelloMessage());
                if (!res.Success)
                {
                    MessageBox.Show("Client version incompatible with selected server.");
                    DialogResult = DialogResult.None;
                    return;
                }
                byte[] publicKeyData = res.PublicKeyData;
                byte[] nonce = res.Nonce;
                Message response = con.SendMessage(new LoginPlayerMessage(player.Name, player.PasswordHash, publicKeyData, nonce));
                LoginPlayerResponseMessage realResponse = response as LoginPlayerResponseMessage;
                if (realResponse != null)
                {
                    if (!realResponse.Success)
                    {
                        if (MessageBox.Show("Username does not exist or password wrong, should I attempt to create a new player?", "Login failure.", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            // TODO - confirm password.
                            response = con.SendMessage(new CreatePlayerMessage(player.Name, player.PasswordHash, publicKeyData, nonce));
                            CreatePlayerResponseMessage realResponse2 = response as CreatePlayerResponseMessage;
                            if (realResponse2 != null)
                            {
                                if (realResponse2.Success)
                                {
                                    MessageBox.Show("Player successfully created.");
                                    return;
                                }
                                else
                                {
                                    MessageBox.Show("Name already exists, choose another.");
                                    DialogResult = DialogResult.None;
                                    return;
                                }
                            }
                            else
                                throw new NotSupportedException("Unexpected response type from server.");
                        }
                        else
                        {
                            DialogResult = DialogResult.None;
                            return;
                        }
                    }
                }
                else
                    throw new NotSupportedException("Unexpected response received from server.");
            }
            catch
            {
                MessageBox.Show("Unexpected error occured while logging in.");
                DialogResult = DialogResult.None;
                return;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {

        }
    }
}