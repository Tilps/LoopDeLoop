using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class ActionHistoryTests
    {
        [TestMethod]
        public void PuzzleEdgeAction_NormalCycle_TransitionsAndUndosCorrectly()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();

            Assert.AreEqual(EdgeState.Empty, mesh.Edges[0].State);

            // Empty -> Filled
            var a1 = new PuzzleEdgeAction(mesh, 0, isAlternativeCycle: false);
            Assert.IsTrue(tree.Do(a1));
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            // Filled -> Excluded
            var a2 = new PuzzleEdgeAction(mesh, 0, isAlternativeCycle: false);
            Assert.IsTrue(tree.Do(a2));
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[0].State);

            // Excluded -> Empty
            var a3 = new PuzzleEdgeAction(mesh, 0, isAlternativeCycle: false);
            Assert.IsTrue(tree.Do(a3));
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[0].State);

            // Undo all 3 steps
            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[0].State);

            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[0].State);

            // Redo all 3 steps
            Assert.IsTrue(tree.Redo());
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            Assert.IsTrue(tree.Redo());
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[0].State);

            Assert.IsTrue(tree.Redo());
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[0].State);
        }

        [TestMethod]
        public void PuzzleEdgeAction_AlternativeCycle_TransitionsCorrectly()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();

            // Alternative cycle (right-click / alt mode): Empty -> Excluded -> Filled -> Empty
            var a1 = new PuzzleEdgeAction(mesh, 0, isAlternativeCycle: true);
            Assert.IsTrue(tree.Do(a1));
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[0].State);

            var a2 = new PuzzleEdgeAction(mesh, 0, isAlternativeCycle: true);
            Assert.IsTrue(tree.Do(a2));
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            var a3 = new PuzzleEdgeAction(mesh, 0, isAlternativeCycle: true);
            Assert.IsTrue(tree.Do(a3));
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[0].State);
        }

        [TestMethod]
        public void PuzzleCellColorAction_NormalAndReverseCycles()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();

            Assert.AreEqual(0, mesh.Cells[0].Color);

            // Normal: 0 -> 1 (Inside) -> -1 (Outside) -> 0
            var c1 = new PuzzleCellColorAction(mesh, 0, reverse: false);
            Assert.IsTrue(tree.Do(c1));
            Assert.AreEqual(1, mesh.Cells[0].Color);

            var c2 = new PuzzleCellColorAction(mesh, 0, reverse: false);
            Assert.IsTrue(tree.Do(c2));
            Assert.AreEqual(-1, mesh.Cells[0].Color);

            var c3 = new PuzzleCellColorAction(mesh, 0, reverse: false);
            Assert.IsTrue(tree.Do(c3));
            Assert.AreEqual(0, mesh.Cells[0].Color);

            // Undo
            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(-1, mesh.Cells[0].Color);
            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(1, mesh.Cells[0].Color);
            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(0, mesh.Cells[0].Color);

            // Reverse cycle on cell 1: 0 -> -1 -> 1 -> 0
            var r1 = new PuzzleCellColorAction(mesh, 1, reverse: true);
            Assert.IsTrue(tree.Do(r1));
            Assert.AreEqual(-1, mesh.Cells[1].Color);

            var r2 = new PuzzleCellColorAction(mesh, 1, reverse: true);
            Assert.IsTrue(tree.Do(r2));
            Assert.AreEqual(1, mesh.Cells[1].Color);

            var r3 = new PuzzleCellColorAction(mesh, 1, reverse: true);
            Assert.IsTrue(tree.Do(r3));
            Assert.AreEqual(0, mesh.Cells[1].Color);
        }

        [TestMethod]
        public void UndoTree_BranchSwitching_MaintainsCorrectActiveHistory()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();

            // Do action A: edge 0 -> Filled
            tree.Do(new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Filled));
            // Do action B: edge 1 -> Filled
            tree.Do(new PuzzleSetEdgeStateAction(mesh, 1, EdgeState.Filled));
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[1].State);

            // Undo back to A
            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[1].State);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);
            Assert.IsTrue(tree.CanRedo); // Redo to B is available

            // Now branch off by doing action C: edge 2 -> Filled
            tree.Do(new PuzzleSetEdgeStateAction(mesh, 2, EdgeState.Filled));
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[1].State);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[2].State);

            // Branch C is current; redo should now be false (at head of new branch)
            Assert.IsFalse(tree.CanRedo);

            // Undo reverts C to A
            Assert.IsTrue(tree.Undo());
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[2].State);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            // Redo advances forward on the current active branch (to C, not abandoned B)
            Assert.IsTrue(tree.Redo());
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[2].State);
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[1].State);
        }

        [TestMethod]
        public void UndoTree_NoOpAction_DoesNotDirtyHistory()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();

            Assert.IsFalse(tree.CanUndo);

            // Edge 0 is currently Empty; setting it to Empty is a no-op
            bool done = tree.Do(new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Empty));
            Assert.IsFalse(done);
            Assert.IsFalse(tree.CanUndo);
        }
    }
}
