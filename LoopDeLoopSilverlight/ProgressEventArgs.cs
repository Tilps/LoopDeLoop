using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoopDeLoop
{
    public class ProgressEventArgs : EventArgs
    {
        public int CurrentPruned { get; private set; }
        public int TargetPruned { get; private set; }

        public ProgressEventArgs(int currentPruned, int targetPruned)
        {
            this.CurrentPruned = currentPruned;
            this.TargetPruned = targetPruned;
        }
    }
}
