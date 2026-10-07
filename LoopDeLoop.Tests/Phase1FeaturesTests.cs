using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;
using LoopDeLoop.Web.Services;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class Phase1FeaturesTests
    {
        [TestMethod]
        public void RecalculateCounts_CorrectlyUpdatesFilledAndExcludedCounts()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            mesh.SetClue(0, 2);

            var cell = mesh.Cells[0];
            int e0 = cell.Edges[0];
            int e1 = cell.Edges[1];
            int e2 = cell.Edges[2];

            // Manually set states directly without events
            mesh.Edges[e0].State = EdgeState.Filled;
            mesh.Edges[e1].State = EdgeState.Excluded;
            mesh.Edges[e2].State = EdgeState.Filled;

            PuzzleHelper.RecalculateCounts(mesh);

            Assert.AreEqual(2, cell.FilledCount, "Filled count must be 2");
            Assert.AreEqual(1, cell.ExcludedCount, "Excluded count must be 1");

            var inter = mesh.Intersections[mesh.Edges[e0].Intersections[0]];
            Assert.IsTrue(inter.FilledCount >= 1, "Intersection filled count must be at least 1");
        }

        [TestMethod]
        public void SolvePuzzle_SolvesMesh_AndCanUndoCleanly()
        {
            var game = new GameService();
            // 2x2 grid where each cell has clue 2 has a unique solution: the outer perimeter
            var mesh = new Mesh(2, 2, MeshType.Square);
            for (int i = 0; i < 4; i++)
            {
                mesh.SetClue(i, 2);
            }
            game.LoadPuzzleText(PuzzleCodec.Encode(mesh, 2, 2));

            Assert.IsNotNull(game.CurrentMesh);
            Assert.IsFalse(game.IsSolved);

            bool solved = game.SolvePuzzle();
            Assert.IsTrue(solved, "SolvePuzzle must succeed on solvable mesh");
            Assert.IsTrue(game.IsSolved, "Game must report IsSolved after solve");
            Assert.IsTrue(game.WasAutoSolved, "WasAutoSolved must be true when solved via SolvePuzzle");

            // Verify Undo reverts the solve
            Assert.IsTrue(game.CanUndo, "Must be able to undo batch solve");
            game.Undo();
            Assert.IsFalse(game.IsSolved, "Must no longer be solved after Undo");
            Assert.IsFalse(game.WasAutoSolved, "WasAutoSolved must reset after Undo");
            Assert.AreEqual(0, game.MarkedEdges.Count, "Victory auto-fix must be rolled back so board is interactable after Undo");

            // Redo of the solve is still an auto-solve (no victory celebration)
            Assert.IsTrue(game.CanRedo);
            game.Redo();
            Assert.IsTrue(game.IsSolved, "Redo must re-apply solution");
            Assert.IsTrue(game.WasAutoSolved, "Redo of a Solve action must still count as auto-solved");

            game.Undo();
            Assert.AreEqual(0, game.MarkedEdges.Count, "Board must be unlocked again after second Undo");
        }

        [TestMethod]
        public void ResetCurrentPuzzle_ClearsStateAndRestarts()
        {
            var game = new GameService();
            var mesh = new Mesh(3, 3, MeshType.Square);
            game.LoadPuzzleText(PuzzleCodec.Encode(mesh, 3, 3));

            // Make some moves
            game.ToggleEdge(0, false);
            game.ToggleEdge(1, false);
            game.Fix();

            Assert.IsTrue(game.MarkedEdges.Count > 0);
            Assert.AreEqual(EdgeState.Filled, game.CurrentMesh!.Edges[0].State);

            game.ResetCurrentPuzzle();

            Assert.AreEqual(0, game.MarkedEdges.Count, "Marked edges must be cleared on reset");
            Assert.IsFalse(game.CanUndo, "Undo tree must be fresh on reset");
            Assert.IsFalse(game.IsSolved, "IsSolved must be false on reset");
            Assert.AreEqual(EdgeState.Empty, game.CurrentMesh.Edges[0].State, "Edges must be Empty after reset");
        }

        [TestMethod]
        public void RestrictInvalid_FlashesInvalidEdgeOnIllegalMove()
        {
            var game = new GameService();
            var mesh = new Mesh(3, 3, MeshType.Square);
            mesh.SetClue(0, 0); // 0 clue: filling any adjacent edge is invalid
            game.LoadPuzzleText(PuzzleCodec.Encode(mesh, 3, 3));
            game.DisallowFalseMove = true;

            int edgeOfZero = game.CurrentMesh!.Cells[0].Edges[0];

            // Attempt to fill an edge of a 0 clue
            game.SetEdgeDirect(edgeOfZero, EdgeState.Filled);

            // Move should be rejected
            Assert.AreEqual(EdgeState.Empty, game.CurrentMesh.Edges[edgeOfZero].State,
                "Filling an edge of a 0-cell must be rejected when DisallowFalseMove is true");
            Assert.AreEqual(edgeOfZero, game.InvalidEdgeIndex,
                "InvalidEdgeIndex must be set to the offending edge for visual flash");
        }

        [TestMethod]
        public void Hint_FindsDeduction_AndDoHintAppliesIt()
        {
            var game = new GameService();
            var mesh = new Mesh(3, 3, MeshType.Square);
            mesh.SetClue(4, 0); // Center cell is 0
            game.LoadPuzzleText(PuzzleCodec.Encode(mesh, 3, 3));

            var (edgeIdx, targetState, message) = game.ComputeHint();

            Assert.IsTrue(edgeIdx.HasValue, "ComputeHint must find a deduction for 0 clue");
            Assert.AreEqual(EdgeState.Excluded, targetState, "Deduction for 0 clue must be Excluded");
            Assert.IsNotNull(message);
            StringAssert.Contains(message, "0", "Message should explain deduction relates to clue 0");

            // Now test RequestHint with applyMove = true
            bool applied = game.RequestHint(applyMove: true);
            Assert.IsTrue(applied);
            Assert.AreEqual(EdgeState.Excluded, game.CurrentMesh!.Edges[edgeIdx.Value].State,
                "DoHint must apply the deduction to CurrentMesh");
            Assert.IsTrue(game.CanUndo, "DoHint must be recorded in undo history");
        }
    }
}
