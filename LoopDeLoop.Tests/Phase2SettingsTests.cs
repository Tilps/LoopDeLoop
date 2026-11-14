using System;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;
using LoopDeLoop.Web.Services;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class Phase2SettingsTests
    {
        [TestMethod]
        public void PuzzleSetEdgeStateAction_WithAutoMove_DeducesAndRollsBackOnUndo()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            // Center cell (index 4 in 3x3) has 4-degree vertices, so excluding 1 edge validly forces the other 3 Filled.
            mesh.SetClue(4, 3);
            var centerCell = mesh.Cells[4];
            int targetEdge = centerCell.Edges[0];

            var action = new PuzzleSetEdgeStateAction(mesh, targetEdge, EdgeState.Excluded, disallowFalseMove: false, autoMove: 1);
            Assert.IsTrue(action.Perform(), "Action with autoMove=1 should succeed");
            Assert.IsTrue(action.Successful, "Valid 3-clue auto-move should mark action.Successful=true");

            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[targetEdge].State);
            for (int i = 1; i < centerCell.Edges.Count; i++)
            {
                Assert.AreEqual(EdgeState.Filled, mesh.Edges[centerCell.Edges[i]].State,
                    $"Excluding 1 edge of center 3-clue square must auto-fill remaining edge {i}");
            }

            action.Unperform();
            foreach (int edgeIdx in centerCell.Edges)
            {
                Assert.AreEqual(EdgeState.Empty, mesh.Edges[edgeIdx].State);
            }
        }

        [TestMethod]
        public void PuzzleSetEdgeStateAction_WithDisallowFalseMoveAndAutoMove_RollsBackOnContradiction()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);
            // Adjacent 3-cell clues sharing an edge: setting two outer edges of cell 0 to Excluded
            // contradicts clue 3, triggering rollback with disallowFalseMove = true.
            mesh.SetClue(0, 3);
            var cell0 = mesh.Cells[0];

            Assert.IsTrue(new PuzzleSetEdgeStateAction(mesh, cell0.Edges[0], EdgeState.Excluded, disallowFalseMove: false, autoMove: 0).Perform());
            var badAction = new PuzzleSetEdgeStateAction(mesh, cell0.Edges[1], EdgeState.Excluded, disallowFalseMove: true, autoMove: 2);

            Assert.IsFalse(badAction.Perform(), "Contradictory move with disallowFalseMove=true must return false");
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[cell0.Edges[0]].State);
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[cell0.Edges[1]].State);
        }

        [TestMethod]
        public void GameService_AutoMove_PropagatesAndSanitizesInteractiveMeshFlags()
        {
            var game = new GameService();
            var mesh = new Mesh(3, 3, MeshType.Square);
            mesh.SetClue(4, 3);
            game.LoadPuzzleText(PuzzleCodec.Encode(mesh, 3, 3));

            Assert.IsNotNull(game.CurrentMesh);
            // Simulate generator flags lingering on the mesh
            game.CurrentMesh.UseColoring = true;
            game.CurrentMesh.UseCellColoring = true;
            game.CurrentMesh.UseDerivedColoring = true;
            game.CurrentMesh.UseMerging = true;
            game.CurrentMesh.ColoringCheats = true;
            game.CurrentMesh.ConsiderMultipleLoops = false;

            game.AutoMoveLevel = 1;
            game.AutoMoveConsiderMultipleLoops = true;
            game.AutoMoveUseIC = true;

            int targetEdge = game.CurrentMesh.Cells[4].Edges[0];
            game.ToggleEdge(targetEdge, isAlternative: true); // Empty -> Excluded

            Assert.AreEqual(EdgeState.Excluded, game.CurrentMesh.Edges[targetEdge].State);
            for (int i = 1; i < game.CurrentMesh.Cells[4].Edges.Count; i++)
            {
                Assert.AreEqual(EdgeState.Filled, game.CurrentMesh.Edges[game.CurrentMesh.Cells[4].Edges[i]].State);
            }

            // Interactive auto-move must override heavy generator solver flags
            Assert.IsFalse(game.CurrentMesh.UseColoring);
            Assert.IsFalse(game.CurrentMesh.UseCellColoring);
            Assert.IsFalse(game.CurrentMesh.UseDerivedColoring);
            Assert.IsFalse(game.CurrentMesh.UseMerging);
            Assert.IsFalse(game.CurrentMesh.ColoringCheats);
            Assert.IsTrue(game.CurrentMesh.ConsiderMultipleLoops);
            Assert.IsTrue(game.CurrentMesh.ConsiderIntersectCellInteractsAsSimple);

            // SetEdgeDirect with autoMove=4 (Full Cascade) should cascade on a second puzzle move
            game.Undo();
            game.AutoMoveLevel = 4;
            game.SetEdgeDirect(targetEdge, EdgeState.Excluded);
            for (int i = 1; i < game.CurrentMesh.Cells[4].Edges.Count; i++)
            {
                Assert.AreEqual(EdgeState.Filled, game.CurrentMesh.Edges[game.CurrentMesh.Cells[4].Edges[i]].State);
            }
        }

        [TestMethod]
        public void GameService_AutoStart_AppliesOpeningDeductionsWhenEnabledOnReset()
        {
            var game = new GameService();
            var mesh = new Mesh(3, 3, MeshType.Square);
            mesh.SetClue(0, 0);
            game.LoadPuzzleText(PuzzleCodec.Encode(mesh, 3, 3));
            Assert.IsNotNull(game.CurrentMesh);

            game.AutoStart = false;
            game.ResetCurrentPuzzle();
            foreach (int e in game.CurrentMesh.Cells[0].Edges)
            {
                Assert.AreEqual(EdgeState.Empty, game.CurrentMesh.Edges[e].State);
            }

            game.AutoStart = true;
            game.ResetCurrentPuzzle();
            foreach (int e in game.CurrentMesh.Cells[0].Edges)
            {
                Assert.AreEqual(EdgeState.Excluded, game.CurrentMesh.Edges[e].State,
                    "AutoStart must automatically exclude edges around a 0 clue");
            }
        }

        [TestMethod]
        public void GameService_GeneratorOverrides_ClampsValuesAndAppliesSolverOverride()
        {
            var game = new GameService
            {
                AutoMoveLevel = 99,
                GenerateLengthFraction = 0.05,
                GenerateBoringFraction = 0.90,
                SolverModeOverride = GeneratorSolverMode.Iterative
            };

            Assert.AreEqual(4, game.AutoMoveLevel);
            Assert.AreEqual(0.25, game.GenerateLengthFraction, 1e-6);
            Assert.AreEqual(0.20, game.GenerateBoringFraction, 1e-6);

            game.AutoMoveLevel = -3;
            game.GenerateLengthFraction = 0.99;
            game.GenerateBoringFraction = -0.5;
            Assert.AreEqual(0, game.AutoMoveLevel);
            Assert.AreEqual(0.75, game.GenerateLengthFraction, 1e-6);
            Assert.AreEqual(0.0, game.GenerateBoringFraction, 1e-6);

            var mesh = new Mesh(4, 4, MeshType.Square) { SolverMethod = SolverMethod.Recursive };
            game.ApplyGeneratorOverrides(mesh);
            Assert.AreEqual(0.75, mesh.GenerateLengthFraction, 1e-6);
            Assert.AreEqual(0.0, mesh.GenerateBoringFraction, 1e-6);
            Assert.AreEqual(SolverMethod.Iterative, mesh.SolverMethod);

            game.SolverModeOverride = GeneratorSolverMode.Recursive;
            game.ApplyGeneratorOverrides(mesh);
            Assert.AreEqual(SolverMethod.Recursive, mesh.SolverMethod);
        }

        [TestMethod]
        public void GameService_Preferences_ExportApplyAndResetToDefaults()
        {
            var game = new GameService
            {
                DisallowFalseMove = true,
                ShowCellColors = true,
                AutoMoveLevel = 3,
                AutoMoveConsiderMultipleLoops = false,
                AutoMoveUseIC = true,
                AutoStart = true,
                WheelMode = MouseWheelMode.UndoRedo,
                ShowClock = false,
                GenerateLengthFraction = 0.65,
                GenerateBoringFraction = 0.08,
                SolverModeOverride = GeneratorSolverMode.Recursive
            };

            UserPreferences exported = game.ExportPreferences();
            var restoredGame = new GameService();
            restoredGame.ApplyPreferences(exported);

            Assert.IsTrue(restoredGame.DisallowFalseMove);
            Assert.IsTrue(restoredGame.ShowCellColors);
            Assert.AreEqual(3, restoredGame.AutoMoveLevel);
            Assert.IsFalse(restoredGame.AutoMoveConsiderMultipleLoops);
            Assert.IsTrue(restoredGame.AutoMoveUseIC);
            Assert.IsTrue(restoredGame.AutoStart);
            Assert.AreEqual(MouseWheelMode.UndoRedo, restoredGame.WheelMode);
            Assert.IsFalse(restoredGame.ShowClock);
            Assert.AreEqual(0.65, restoredGame.GenerateLengthFraction, 1e-6);
            Assert.AreEqual(0.08, restoredGame.GenerateBoringFraction, 1e-6);
            Assert.AreEqual(GeneratorSolverMode.Recursive, restoredGame.SolverModeOverride);

            restoredGame.ResetPreferencesToDefaults();

            Assert.IsFalse(restoredGame.DisallowFalseMove);
            Assert.IsFalse(restoredGame.ShowCellColors);
            Assert.AreEqual(0, restoredGame.AutoMoveLevel);
            Assert.IsTrue(restoredGame.AutoMoveConsiderMultipleLoops);
            Assert.IsFalse(restoredGame.AutoMoveUseIC);
            Assert.IsFalse(restoredGame.AutoStart);
            Assert.AreEqual(MouseWheelMode.Zoom, restoredGame.WheelMode);
            Assert.IsTrue(restoredGame.ShowClock);
            Assert.AreEqual(0.5, restoredGame.GenerateLengthFraction, 1e-6);
            Assert.AreEqual(0.01, restoredGame.GenerateBoringFraction, 1e-6);
            Assert.AreEqual(GeneratorSolverMode.Auto, restoredGame.SolverModeOverride);

            // Verify backwards compatibility deserializing empty/legacy SavedGameState JSON
            var legacyState = JsonSerializer.Deserialize<SavedGameState>("{\"Puzzle\":\"sq/2x2/\"}");
            Assert.IsNotNull(legacyState);
            Assert.AreEqual("sq/2x2/", legacyState.Puzzle);
        }
    }
}
