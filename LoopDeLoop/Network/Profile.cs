using System;
using System.Collections.Generic;
using System.Text;

namespace LoopDeLoop.Network
{
    class Profile
    {
        /// <summary>
        /// Setup default profile.
        /// </summary>
        public Profile()
        {
            BoardStyle = MeshType.Square;
            BoardWidth = 10;
            BoardHeight = 10;
            GeneratorStyle = SolverMethod.Iterative;
            IterativeGeneratorDepth = 1;
            GeneratorCellIntersInteract = false;
            LineToCrossRatio = 2;
            ErrorRatio = 4;
        }

        public MeshType BoardStyle;

        public int BoardWidth;

        public int BoardHeight;

        public SolverMethod GeneratorStyle;

        public int IterativeGeneratorDepth;

        public bool GeneratorCellIntersInteract;

        public bool GenerateConsiderMultipleLoops;

        public double LineToCrossRatio;

        public double ErrorRatio;

        // TODO: Turn based - turn based time length - early end - automove - auto move scoring method - autostart - appearing numbers mode - appearing numbers timer length
        // And PLayers count, with -1 for no limit.

    }
}
