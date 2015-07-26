using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LoopDeLoop.Network
{
    class Shard
    {
        public Dictionary<string, Lobby> Lobbies = new Dictionary<string, Lobby>();

        public event LogEventHandler LogOccurred;

        internal virtual void ConnectionClosed(Connection connection)
        {
        }

        internal void Log(string message)
        {
            try
            {
                if (LogOccurred != null)
                    LogOccurred(this, new LogEventArgs(message));
            }
            catch
            {
            }
        }
    }

    delegate void LogEventHandler(object sender, LogEventArgs args);

    class LogEventArgs : EventArgs
    {
        public LogEventArgs(string message)
        {
            this.Message = message;
        }
        public string Message;
    }
}
