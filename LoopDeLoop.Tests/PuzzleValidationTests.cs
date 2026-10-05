using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class PuzzleValidationTests
    {
        [TestMethod]
        public void NullOrEmptyMesh_ReturnsFalse()
        {
            Assert.IsFalse(PuzzleHelper.CheckIsSolved(null!));
            Assert.IsFalse(PuzzleHelper.CheckIsSolved(new Mesh(0, 0, MeshType.Square)));
        }

        [TestMethod]
        public void MeshWithNoClues_ReturnsFalse()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);
            // All cell TargetCounts are default -1
            Assert.IsFalse(PuzzleHelper.CheckIsSolved(mesh));
        }

        [TestMethod]
        public void ValidPerimeterLoop_ReturnsTrue()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);

            // In a 2x2 square grid, boundary edges have exactly 1 neighboring cell.
            // There are 8 boundary edges forming a single outer square loop.
            for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
            {
                if (mesh.Edges[eIdx].Cells.Count == 1)
                {
                    new SetAction(mesh, eIdx, EdgeState.Filled).Perform();
                }
            }

            // Each of the 4 corner cells has 2 boundary edges filled.
            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                mesh.SetClue(i, mesh.Cells[i].FilledCount);
            }

            Assert.IsTrue(PuzzleHelper.CheckIsSolved(mesh),
                "Single valid 8-edge loop matching all clues should be recognized as Solved");
        }

        [TestMethod]
        public void DeadEnd_ReturnsFalse()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);
            var boundaryEdges = mesh.Edges.Select((e, idx) => (e, idx)).Where(t => t.e.Cells.Count == 1).Select(t => t.idx).ToList();

            // Fill only 7 of the 8 perimeter edges, leaving open endpoints (degree 1)
            for (int i = 0; i < 7; i++)
            {
                new SetAction(mesh, boundaryEdges[i], EdgeState.Filled).Perform();
            }

            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                mesh.SetClue(i, mesh.Cells[i].FilledCount);
            }

            Assert.IsFalse(PuzzleHelper.CheckIsSolved(mesh),
                "Open path with dead ends must not be recognized as Solved");
        }

        [TestMethod]
        public void BranchingVertexDegree3_ReturnsFalse()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);

            // Fill all 8 boundary edges
            for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
            {
                if (mesh.Edges[eIdx].Cells.Count == 1)
                    new SetAction(mesh, eIdx, EdgeState.Filled).Perform();
            }

            // Also fill 1 interior edge, creating a T-junction (degree 3) at two vertices
            int interiorEdgeIdx = mesh.Edges.FindIndex(e => e.Cells.Count == 2);
            new SetAction(mesh, interiorEdgeIdx, EdgeState.Filled).Perform();

            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                mesh.SetClue(i, mesh.Cells[i].FilledCount);
            }

            Assert.IsFalse(PuzzleHelper.CheckIsSolved(mesh),
                "Fork / branching vertex with degree > 2 must not be recognized as Solved");
        }

        [TestMethod]
        public void ClueMismatch_OverfilledOrUnderfilled_ReturnsFalse()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);

            for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
            {
                if (mesh.Edges[eIdx].Cells.Count == 1)
                    new SetAction(mesh, eIdx, EdgeState.Filled).Perform();
            }

            // Cell 0 actually has 2 filled edges
            // Set TargetCount to 3 (underfilled)
            mesh.SetClue(0, 3);
            mesh.SetClue(1, mesh.Cells[1].FilledCount);
            mesh.SetClue(2, mesh.Cells[2].FilledCount);
            mesh.SetClue(3, mesh.Cells[3].FilledCount);

            Assert.IsFalse(PuzzleHelper.CheckIsSolved(mesh), "Underfilled cell should fail validation");

            // Set TargetCount to 1 (overfilled)
            mesh.SetClue(0, 1);
            Assert.IsFalse(PuzzleHelper.CheckIsSolved(mesh), "Overfilled cell should fail validation");
        }

        [TestMethod]
        public void DisjointMultipleLoops_ReturnsFalse()
        {
            // In a 4x4 square grid, create two separate 1x1 loops
            // Top-left cell (cell 0) and bottom-right cell (cell 15)
            var mesh = new Mesh(4, 4, MeshType.Square);

            // Fill cell 0's 4 edges
            foreach (int eIdx in mesh.Cells[0].Edges)
            {
                new SetAction(mesh, eIdx, EdgeState.Filled).Perform();
            }

            // Fill cell 15's 4 edges (which don't share any edges or vertices with cell 0)
            int lastCell = mesh.Cells.Count - 1;
            foreach (int eIdx in mesh.Cells[lastCell].Edges)
            {
                new SetAction(mesh, eIdx, EdgeState.Filled).Perform();
            }

            // Set clues matching filled counts
            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                if (mesh.Cells[i].FilledCount > 0)
                    mesh.SetClue(i, mesh.Cells[i].FilledCount);
            }

            // Each loop has degree 2 at all vertices, and clues match.
            // BUT there are two separate loops, not a single connected loop!
            Assert.IsFalse(PuzzleHelper.CheckIsSolved(mesh),
                "Multiple disconnected loops must not pass single-loop check");
        }
    }
}
