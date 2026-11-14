using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;
using LoopDeLoop.Web.Services;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class Phase3VisualSolverTests
    {
        private static Mesh CreateSolvable3x3Puzzle()
        {
            // Unique 3x3 puzzle: center 3-clue cornered by 0 clues on diagonal/opposite sides,
            // e.g., simple 2x2 all-2s perimeter (fast, deterministic trace).
            var mesh = new Mesh(2, 2, MeshType.Square);
            for (int i = 0; i < 4; i++)
            {
                mesh.SetClue(i, 2);
            }
            return mesh;
        }

        [TestMethod]
        public void StartVisualSolve_CapturesStepTraceAndEndsWithSolvedState()
        {
            var game = new GameService();
            game.LoadPuzzleText(PuzzleCodec.Encode(CreateSolvable3x3Puzzle(), 2, 2));
            Assert.IsNotNull(game.CurrentMesh);

            bool started = game.StartVisualSolve();
            Assert.IsTrue(started, "StartVisualSolve should succeed on a solvable mesh");
            Assert.IsTrue(game.IsVisualSolving);
            Assert.IsGreaterThanOrEqualTo(2, game.VisualSolveTotalSteps, "Trace should include start and solved steps");
            Assert.AreEqual(0, game.VisualSolveStepIndex);
            Assert.AreEqual("Starting solver...", game.CurrentVisualSolveStep?.Status);
            Assert.AreEqual("Solution complete!", game.VisualSolveSteps[^1].Status);

            // Board itself must remain untouched during visual preview
            Assert.IsFalse(game.IsSolved);
            for (int i = 0; i < game.CurrentMesh.Edges.Count; i++)
            {
                Assert.AreEqual(EdgeState.Empty, game.CurrentMesh.Edges[i].State);
            }

            // Final trace frame should represent the full solved loop
            var finalStates = game.VisualSolveSteps[^1].EdgeStates;
            var checkMesh = new Mesh(game.CurrentMesh);
            for (int i = 0; i < finalStates.Length; i++)
            {
                if (finalStates[i] != EdgeState.Empty)
                    new SetAction(checkMesh, i, finalStates[i]).Perform();
            }
            Assert.IsTrue(PuzzleHelper.CheckIsSolved(checkMesh), "Final trace frame must be a valid complete loop");
        }

        [TestMethod]
        public void VisualSolve_StepNavigation_ClampsAndStepsCleanly()
        {
            var game = new GameService();
            game.LoadPuzzleText(PuzzleCodec.Encode(CreateSolvable3x3Puzzle(), 2, 2));
            Assert.IsTrue(game.StartVisualSolve());

            int total = game.VisualSolveTotalSteps;
            Assert.IsFalse(game.StepVisualSolveBackward(), "Cannot step backward past step 0");
            Assert.IsTrue(game.StepVisualSolveForward(), "Should advance from step 0");
            Assert.AreEqual(1, game.VisualSolveStepIndex);

            game.SetVisualSolveStep(9999);
            Assert.AreEqual(total - 1, game.VisualSolveStepIndex);
            Assert.IsFalse(game.StepVisualSolveForward(), "Cannot step forward past last frame");

            game.SetVisualSolveStep(-5);
            Assert.AreEqual(0, game.VisualSolveStepIndex);
        }

        [TestMethod]
        public void VisualSolve_AutoDismisses_OnUserMoveUndoResetOrFix()
        {
            var game = new GameService();
            game.LoadPuzzleText(PuzzleCodec.Encode(CreateSolvable3x3Puzzle(), 2, 2));
            Assert.IsNotNull(game.CurrentMesh);

            // Place one player edge before launching Watch Solver
            game.ToggleEdge(0, isAlternative: false);
            Assert.AreEqual(EdgeState.Filled, game.CurrentMesh.Edges[0].State);

            // Dismissing via Undo reverts that edge cleanly and exits playback
            Assert.IsTrue(game.StartVisualSolve());
            Assert.IsTrue(game.IsVisualSolving);
            game.Undo();
            Assert.IsFalse(game.IsVisualSolving);
            Assert.AreEqual(EdgeState.Empty, game.CurrentMesh.Edges[0].State);

            // Dismissing via ToggleEdge exits playback and keeps player interaction working
            Assert.IsTrue(game.StartVisualSolve());
            game.ToggleEdge(1, isAlternative: false);
            Assert.IsFalse(game.IsVisualSolving);
            Assert.AreEqual(EdgeState.Filled, game.CurrentMesh.Edges[1].State);

            // Dismissing via Reset clears board and exits playback
            Assert.IsTrue(game.StartVisualSolve());
            game.ResetCurrentPuzzle();
            Assert.IsFalse(game.IsVisualSolving);
            Assert.AreEqual(EdgeState.Empty, game.CurrentMesh.Edges[1].State);

            // Explicit DismissVisualSolve works too
            Assert.IsTrue(game.StartVisualSolve());
            game.DismissVisualSolve();
            Assert.IsFalse(game.IsVisualSolving);
        }

        [TestMethod]
        public void ApplyVisualSolveSolution_CommitsUndoableSolveAndDismissesPlayback()
        {
            var game = new GameService();
            game.LoadPuzzleText(PuzzleCodec.Encode(CreateSolvable3x3Puzzle(), 2, 2));

            Assert.IsTrue(game.StartVisualSolve());
            Assert.IsTrue(game.ApplyVisualSolveSolution());

            Assert.IsFalse(game.IsVisualSolving, "Applying solution must dismiss playback bar");
            Assert.IsTrue(game.IsSolved);
            Assert.IsTrue(game.WasAutoSolved);
            Assert.IsTrue(game.CanUndo);

            game.Undo();
            Assert.IsFalse(game.IsSolved);
            Assert.IsFalse(game.WasAutoSolved);
        }
    }
}
