using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class SolverDeductionTests
    {
        [TestMethod]
        public void ZeroClue_DeducesAllIncidentEdgesExcluded()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            mesh.SetClue(0, 0);

            var changes = new List<IAction>();
            bool startOk = mesh.PerformStart(changes);
            Assert.IsTrue(startOk);

            // All 4 edges around the 0 cell must be Excluded
            foreach (int eIdx in mesh.Cells[0].Edges)
            {
                Assert.AreEqual(EdgeState.Excluded, mesh.Edges[eIdx].State,
                    $"Edge {eIdx} of a 0-cell must be deduced as Excluded");
            }
        }

        [TestMethod]
        public void CellTargetReached_DeducesRemainingCellEdgesExcluded()
        {
            // Use center cell (cell 4) of 3x3 grid to avoid corner boundary interactions
            var mesh = new Mesh(3, 3, MeshType.Square);
            int centerCell = 4;
            mesh.SetClue(centerCell, 2);

            var changes = new List<IAction>();
            mesh.PerformStart(changes);

            var cellEdges = mesh.Cells[centerCell].Edges;
            // Fill 2 of the 4 edges of the center cell
            bool ok1 = mesh.Perform(cellEdges[0], EdgeState.Filled, changes);
            Assert.IsTrue(ok1);
            bool ok2 = mesh.Perform(cellEdges[1], EdgeState.Filled, changes);
            Assert.IsTrue(ok2);

            // The remaining 2 edges of the cell must be deduced as Excluded
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[cellEdges[2]].State,
                "Once cell target is reached, remaining edges must be Excluded");
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[cellEdges[3]].State,
                "Once cell target is reached, remaining edges must be Excluded");
        }

        [TestMethod]
        public void CellForced_DeducesRemainingCellEdgesFilled()
        {
            // Use center cell (cell 4) of 3x3 grid which has 4 unconstrained degree-4 vertices
            var mesh = new Mesh(3, 3, MeshType.Square);
            int centerCell = 4;
            mesh.SetClue(centerCell, 3);

            var changes = new List<IAction>();
            mesh.PerformStart(changes);

            var cellEdges = mesh.Cells[centerCell].Edges;
            // Exclude 1 edge of cell 4
            bool ok = mesh.Perform(cellEdges[0], EdgeState.Excluded, changes);
            Assert.IsTrue(ok);

            // Since only 3 edges remain available for a 3-clue, all 3 must be deduced as Filled
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[cellEdges[1]].State);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[cellEdges[2]].State);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[cellEdges[3]].State);
        }

        [TestMethod]
        public void CornerVertexDegree2_FillingOneEdge_ForcesOtherEdgeFilled()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            // Corner intersection 0 has exactly 2 incident edges
            Assert.AreEqual(2, mesh.Intersections[0].Edges.Count);

            int e1 = mesh.Intersections[0].Edges[0];
            int e2 = mesh.Intersections[0].Edges[1];

            var changes = new List<IAction>();
            bool ok = mesh.Perform(e1, EdgeState.Filled, changes);
            Assert.IsTrue(ok);

            // Degree 2 requirement forces the other corner edge to be Filled immediately
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[e2].State,
                "In a degree-2 corner vertex, filling one edge must deduce the other as Filled");
        }

        [TestMethod]
        public void IntersectionDegree2_DeducesRemainingIncidentEdgesExcluded()
        {
            // Pick an interior intersection in 3x3 grid with 4 incident edges
            var mesh = new Mesh(3, 3, MeshType.Square);
            int interiorInterIdx = -1;
            for (int i = 0; i < mesh.Intersections.Count; i++)
            {
                if (mesh.Intersections[i].Edges.Count == 4)
                {
                    interiorInterIdx = i;
                    break;
                }
            }
            Assert.IsTrue(interiorInterIdx >= 0);

            var interEdges = mesh.Intersections[interiorInterIdx].Edges;
            var changes = new List<IAction>();

            // Fill 2 incident edges
            mesh.Perform(interEdges[0], EdgeState.Filled, changes);
            mesh.Perform(interEdges[1], EdgeState.Filled, changes);

            // Since vertex degree is now 2, the remaining 2 incident edges must be Excluded
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[interEdges[2]].State,
                "Once vertex degree 2 is reached, remaining incident edges must be Excluded");
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[interEdges[3]].State,
                "Once vertex degree 2 is reached, remaining incident edges must be Excluded");
        }

        [TestMethod]
        public void UnsolvablePuzzle_ReturnsNoSolutions()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);

            // Three 0-cells exclude almost all edges, leaving cell 3 with only 2 possible edges
            mesh.SetClue(0, 0);
            mesh.SetClue(1, 0);
            mesh.SetClue(2, 0);
            mesh.SetClue(3, 3); // Needs 3 edges, but only 2 remain unexcluded

            var state = mesh.TrySolve();
            Assert.AreEqual(SolveState.NoSolutions, state,
                "Impossible puzzle with contradictory clue must return NoSolutions");
        }

        [TestMethod]
        public void AmbiguousPuzzle_ReturnsMultipleSolutions()
        {
            // A 3x3 grid with only a single clue (e.g., center cell = 2) is under-constrained
            var mesh = new Mesh(3, 3, MeshType.Square);
            mesh.SetClue(4, 2);

            var state = mesh.TrySolve();
            Assert.AreEqual(SolveState.MultipleSolutions, state,
                "Under-constrained puzzle must return MultipleSolutions");
        }
    }
}

